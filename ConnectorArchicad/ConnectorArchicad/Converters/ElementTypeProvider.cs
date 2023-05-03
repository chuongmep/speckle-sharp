﻿using System;
using System.Collections.Generic;
using Beam = Objects.BuiltElements.Archicad.ArchicadBeam;
using Column = Objects.BuiltElements.Archicad.ArchicadColumn;
using DirectShape = Objects.BuiltElements.Archicad.DirectShape;
using Door = Objects.BuiltElements.Archicad.ArchicadDoor;
using Floor = Objects.BuiltElements.Archicad.ArchicadFloor;
using Roof = Objects.BuiltElements.Archicad.ArchicadRoof;
using Room = Objects.BuiltElements.Archicad.ArchicadRoom;
using Shell = Objects.BuiltElements.Archicad.ArchicadShell;
using Wall = Objects.BuiltElements.Archicad.ArchicadWall;
using Window = Objects.BuiltElements.Archicad.ArchicadWindow;

namespace Archicad
{
  public static class ElementTypeProvider
  {
    private static Dictionary<string, Type> _nameToType = new()  {
      { "Wall", typeof(Wall) },
      { "Slab", typeof(Floor) },
      { "Roof", typeof(Roof) },
      { "Shell", typeof(Shell) },
      { "Zone", typeof(Room) },
      { "Beam", typeof(Beam) },
      { "Column", typeof(Column) },
      { "Door", typeof(Door) },
      { "Window", typeof(Window) }

    };

    public static Type GetTypeByName(string name)
    {
      return _nameToType.TryGetValue(name, out var value) ? value : typeof(DirectShape);
    }
  }
}
