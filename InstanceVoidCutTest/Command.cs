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
    /// Retrieve cutting symbol, loading family if needed.
    /// </summary>
    static FamilySymbol RetrieveOrLoadCuttingSymbol( 
      Document doc )
    {
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

          return null;
        }

        // Load family from file:

        using( Transaction tx = new Transaction( 
          doc ) )
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

      return cuttingSymbol;
    }

    /// <summary>
    /// Cut a beam with three instances of a void
    /// cutting family. Its family parameter "Cut 
    /// with Voids When Loaded" must be set to true.
    /// </summary>
    static void CutBeamWithVoid(
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

      int n = 3;
      XYZ p;
      string parameter_name;
      ElementId[] ids = new ElementId[n];

      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Create Cutting Instances" );

        for( int i = 1; i <= n; ++i )
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

          ids[i - 1] = cuttingInstance.Id;
        }
        tx.Commit();
      }

      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Cut Beam With Voids" );

        for( int i = 0; i < n; ++i )
        {
          InstanceVoidCutUtils.AddInstanceVoidCut(
            doc, beam, doc.GetElement( ids[i] ) );
        }
        tx.Commit();
      }
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      using( TransactionGroup g
        = new TransactionGroup( doc ) )
      {
        g.Start( "Cut Beam with Voids" );

        // Retrieve or load cutting symbol

        FamilySymbol cuttingSymbol
          = RetrieveOrLoadCuttingSymbol( doc );

        // Select beam to cut

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

        // Place cutting instances and apply cuts

        CutBeamWithVoid( beam, cuttingSymbol );

        g.Assimilate();

        // Calling Commit after Assimilate throws an 
        // exception saying "The Transaction group has 
        // not been started (its status is not 
        // 'Started').."

        //g.Commit();
      }
      return Result.Succeeded;
    }
  }
}
