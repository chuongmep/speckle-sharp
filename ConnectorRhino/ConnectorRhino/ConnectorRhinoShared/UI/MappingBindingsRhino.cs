﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using DesktopUI2.ViewModels.MappingTool;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Render;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;
using static DesktopUI2.ViewModels.MappingViewModel;
using ApplicationObject = Speckle.Core.Models.ApplicationObject;

namespace SpeckleRhino
{

  public partial class MappingBindingsRhino : MappingsBindings
  {
    static string SpeckleMappingKey = "SpeckleMapping";
    static string SpeckleMappingViewKey = "SpeckleMappingView";

    private MappingsDisplayConduit Display;

    public MappingBindingsRhino()
    {
      Display = new MappingsDisplayConduit();
      Display.Enabled = true;
    }

    public override MappingSelectionInfo GetSelectionInfo()
    {
      var selection = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false).ToList();
      var result = new List<Schema>();

      foreach (var obj in selection)
      {
        var schemas = GetObjectSchemas(obj);

        if (!result.Any())
          result = schemas;
        else
          //intersect lists
          //TODO: if some elements already have a schema and values are different
          //we should default to an empty schema, instead of potentially restoring the one with values
          result = result.Where(x => schemas.Any(y => y.Name == x.Name)).ToList();

        //incompatible selection
        if (!result.Any())
          return new MappingSelectionInfo(new List<Schema>(), selection.Count);
      }

      return new MappingSelectionInfo(result, selection.Count);
    }

    /// <summary>
    /// For a give Rhino Object find all applicable schemas and retrive any existing one already applied
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    private List<Schema> GetObjectSchemas(RhinoObject obj)
    {
      var result = new List<Schema>();

      var existingSchema = GetExistingObjectSchema(obj);
      if (existingSchema != null)
        result.Add(existingSchema);


      switch (obj.Geometry)
      {
        case Mesh m:
          if (!result.Any(x => typeof(DirectShapeFreeformViewModel) == x.GetType()))
            result.Add(new DirectShapeFreeformViewModel());
          break;

        case Brep b:
          if (!result.Any(x => typeof(DirectShapeFreeformViewModel) == x.GetType()))
            result.Add(new DirectShapeFreeformViewModel());
          break;
        //case Brep b:
        //  if (b.IsSurface) cats.Add(DirectShape); // TODO: Wall by face, totally faking it right now
        //  else cats.Add(DirectShape);
        //  break;
        case Extrusion e:
          if (e.ProfileCount > 1) break;
          var crv = e.Profile3d(new ComponentIndex(ComponentIndexType.ExtrusionBottomProfile, 0));
          if (!(crv.IsLinear() || crv.IsArc())) break;
          if (crv.PointAtStart.Z != crv.PointAtEnd.Z) break;

          if (!result.Any(x => typeof(RevitWallViewModel) == x.GetType()))
            result.Add(new RevitWallViewModel());
          break;

          //case Curve c:
          //  if (c.IsLinear()) cats.Add(Beam);
          //  if (c.IsLinear() && c.PointAtEnd.Z == c.PointAtStart.Z) cats.Add(Gridline);
          //  if (c.IsLinear() && c.PointAtEnd.X == c.PointAtStart.X && c.PointAtEnd.Y == c.PointAtStart.Y) cats.Add(Column);
          //  if (c.IsArc() && !c.IsCircle() && c.PointAtEnd.Z == c.PointAtStart.Z) cats.Add(Gridline);
          //  break;
      }

      return result;
    }

    private Schema GetExistingObjectSchema(RhinoObject obj)
    {
      var viewModel = obj.Attributes.GetUserString(SpeckleMappingViewKey);

      if (string.IsNullOrEmpty(viewModel))
        return null;

      try
      {
        var settings = new JsonSerializerSettings()
        {
          TypeNameHandling = TypeNameHandling.All
        };

        return JsonConvert.DeserializeObject<Schema>(viewModel, settings);

      }
      catch
      {
        return null;
      }
    }

    public override void SetMappings(string schema, string viewModel)
    {
      var selection = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false).ToList();
      foreach (var obj in selection)
      {
        obj.Attributes.SetUserString(SpeckleMappingKey, schema);
        obj.Attributes.SetUserString(SpeckleMappingViewKey, viewModel);
      }

      SpeckleRhinoConnectorPlugin.Instance.ExistingSchemaLogExpired = true;

    }

    public override void ClearMappings()
    {
      var selection = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false).ToList();

      foreach (var obj in selection)
      {
        try
        {
          obj.Attributes.DeleteUserString(SpeckleMappingKey);
          obj.Attributes.DeleteUserString(SpeckleMappingViewKey);
        }
        catch { }
      }
      SpeckleRhinoConnectorPlugin.Instance.ExistingSchemaLogExpired = true;
    }

    public override List<Schema> GetExistingSchemaElements()
    {
      var settings = new JsonSerializerSettings()
      {
        TypeNameHandling = TypeNameHandling.All
      };

      var objects = RhinoDoc.ActiveDoc.Objects.FindByUserString(SpeckleMappingViewKey, "*", true);
      var schemas = objects.Select(obj => JsonConvert.DeserializeObject<Schema>(obj.Attributes.GetUserString(SpeckleMappingViewKey), settings)).ToList();


      //add the object id to the schema so we can easily highlight/clear them
      for (var i = 0; i < schemas.Count; i++)
      {
        schemas[i].ApplicationId = objects[i].Id.ToString();
      }


      return schemas;
    }

    public override void HighlightElements(List<string> ids)
    {
      try
      {
        Display.ObjectIds = ids;
        RhinoDoc.ActiveDoc?.Views.Redraw();
      }
      catch (Exception ex)
      {
        //fail silently
      }
    }

    public override void SelectElements(List<string> ids)
    {
      try
      {
        RhinoDoc.ActiveDoc.Objects.Select(ids.Select(x => Guid.Parse(x)));
      }
      catch (Exception ex)
      {
        //fail silently
      }
    }
  }
}
