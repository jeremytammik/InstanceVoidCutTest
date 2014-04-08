#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Structure;
using System.IO;
#endregion

namespace InstanceVoidCutTest
{
  [Transaction( TransactionMode.Manual )]
  public class Command : IExternalCommand
  {
    const string FamilyName = "Cutter";

    const string FamilyPath = "C:/a/vs/InstanceVoidCutTest/Cutter.rfa";

    public static void ErrorMsg( string msg )
    {
      Debug.WriteLine( msg );
      TaskDialog.Show( "InstanceVoidCutUtils Test", msg );
    }

    /// <summary>
    /// Return a string for a real number
    /// formatted to two decimal places.
    /// </summary>
    public static string RealString( double a )
    {
      return a.ToString( "0.##" );
    }

    /// <summary>
    /// Return a string for an XYZ point
    /// or vector with its coordinates
    /// formatted to two decimal places.
    /// </summary>
    public static string PointString( XYZ p )
    {
      return string.Format( "({0},{1},{2})",
        RealString( p.X ),
        RealString( p.Y ),
        RealString( p.Z ) );
    }

    public class BeamSelectionFilter : ISelectionFilter
    {
      public bool AllowElement( Element e )
      {
        return e is FamilyInstance
          && null != e.Category
          && e.Category.Id.IntegerValue.Equals(
            (int) BuiltInCategory.OST_StructuralFraming );
      }

      public bool AllowReference( Reference r, XYZ p )
      {
        return true;
      }
    }

    /// <summary>
    /// Cut a beam with 3 instances of a void-cutting 
    /// family. The Family Parameter "Cut with Voids 
    /// When Loaded" must be true for the cutting family.
    /// </summary>
    void CutBeamWithVoid(
      FamilyInstance beam,
      FamilySymbol cuttingSymbol )
    {
      Document doc = beam.Document;

      Level level = doc.GetElement( beam.LevelId )
        as Level;

      LocationCurve lc = beam.Location
        as LocationCurve;

      Curve beamCurve = lc.Curve;

      Debug.Print( "Beam location from {0} to {1}.",
        PointString( beamCurve.GetEndPoint( 0 ) ),
        PointString( beamCurve.GetEndPoint( 1 ) ) );

      XYZ p;
      string parameter_name;

      for( int i = 1; i <= 3; ++i )
      {
        // Position on beam for this cutting instance

        p = beamCurve.Evaluate( i * 0.25, true );

        // Adjust height for top-aligned curve

        //p = p - XYZ.BasisZ;

        Debug.Print(
          "Family instance insertion at {0}.",
          PointString( p ) );

        FamilyInstance cuttingInstance = doc.Create
          .NewFamilyInstance( p, cuttingSymbol,
            level, StructuralType.NonStructural );

        parameter_name = "A" + i.ToString();

        cuttingInstance
          .get_Parameter( parameter_name )
          .Set( 0.5 * Math.PI );

        // This throws and exception saying 
        // "The element is not a family instance with 
        // an unattached void that can cut. Parameter 
        // name: cuttingInstance"

        InstanceVoidCutUtils.AddInstanceVoidCut(
          doc, beam, cuttingInstance );
      }
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      FilteredElementCollector a
         = new FilteredElementCollector( doc )
           .OfClass( typeof( Family ) );

      Family family = a.FirstOrDefault<Element>(
        e => e.Name.Equals( FamilyName ) )
          as Family;

      if( null == family )
      {
        // It is not present, so check for 
        // the file to load it from:

        if( !File.Exists( FamilyPath ) )
        {
          ErrorMsg( string.Format(
            "Please ensure that the void cutter "
            + "family file '{0}' is present.",
            FamilyPath ) );

          return Result.Failed;
        }

        // Load family from file:

        using( Transaction tx = new Transaction( doc ) )
        {
          tx.Start( "Load Family" );
          doc.LoadFamily( FamilyPath, out family );
          tx.Commit();
        }
      }

      FamilySymbol cuttingSymbol = null;

      foreach( FamilySymbol s in family.Symbols )
      {
        cuttingSymbol = s;
        break;
      }

      Selection sel = uidoc.Selection;

      FamilyInstance beam = null;

      try
      {
        Reference r = sel.PickObject(
          ObjectType.Element,
          new BeamSelectionFilter(),
          "Pick beam to cut" );

        beam = doc.GetElement( r.ElementId )
          as FamilyInstance;
      }
      catch( Autodesk.Revit.Exceptions
        .OperationCanceledException )
      {
        return Result.Cancelled;
      }

      // Modify document within a transaction

      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Cut Beam With Void" );
        CutBeamWithVoid( beam, cuttingSymbol );
        tx.Commit();
      }

      return Result.Succeeded;
    }
  }
}
