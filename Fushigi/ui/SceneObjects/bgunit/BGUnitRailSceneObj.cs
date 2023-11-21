﻿using Fushigi.course;
using Fushigi.gl;
using Fushigi.ui.widgets;
using Fushigi.util;
using ImGuiNET;
using Microsoft.VisualBasic;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Fushigi.ui.SceneObjects.bgunit
{
    //TODO make this into a proper scene object that creates/updates it's points on update
    //via AddOrUpdateChildObject
    //and isn't referenced in CourseUnit.cs
    internal class BGUnitRailSceneObj
    {
        public List<RailPoint> Points = new List<RailPoint>();

        public List<RailPoint> GetSelected(CourseAreaEditContext ctx) => Points.Where(x => ctx.IsSelected(x)).ToList();

        public bool IsClosed = false;

        public bool IsInternal = false;

        public bool mouseDown = false;
        public bool transformStart = false;

        public bool Visible = true;

        public uint Color_Default = 0xFFFFFFFF;
        public uint Color_SelectionEdit = ImGui.ColorConvertFloat4ToU32(new(0.84f, .437f, .437f, 1));
        public uint Color_SlopeError = 0xFF0000FF;

        private Vector3 mouseDownPos;

        public CourseUnit CourseUnit;

        public BGUnitRailSceneObj(CourseUnit unit, CourseUnit.Rail rail)
        {
            CourseUnit = unit;

            Points.Clear();

            foreach (var pt in rail.mPoints)
            {
                var railPoint = new RailPoint(pt.Value);
                railPoint.Transform.Update += unit.GenerateTileSubUnits;
                Points.Add(railPoint);

            }

            IsClosed = rail.IsClosed;
            IsInternal = rail.IsInternal;
        }

        public void Reverse()
        {
            Points.Reverse();
        }

        public CourseUnit.Rail Save()
        {
            CourseUnit.Rail rail = new CourseUnit.Rail()
            {
                IsClosed = IsClosed,
                IsInternal = IsInternal,
                mPoints = new List<Vector3?>(),
            };

            rail.mPoints = new List<Vector3?>();
            foreach (var pt in Points)
                rail.mPoints.Add(pt.Position);

            return rail;
        }

        public void DeselectAll(CourseAreaEditContext ctx)
        {
            foreach (var point in Points)
                if (ctx.IsSelected(point))
                    ctx.Deselect(point);
        }

        public void SelectAll(CourseAreaEditContext ctx)
        {
            foreach (var point in Points)
                ctx.Select(point);
        }

        public void InsertPoint(LevelViewport viewport, RailPoint point, int index)
        {
            Points.Insert(index, point);
            viewport.mEditContext.CommitAction(new UnitRailPointAddUndo(this, point, index));
            viewport.mEditContext.Select(point);
            CourseUnit.GenerateTileSubUnits();
        }

        public void AddPoint(LevelViewport viewport, RailPoint point)
        {
            Points.Add(point);
            viewport.mEditContext.CommitAction(new UnitRailPointAddUndo(this, point));
            viewport.mEditContext.Select(point);
            CourseUnit.GenerateTileSubUnits();
        }

        public void RemoveSelected(LevelViewport viewport)
        {
            var selected = GetSelected(viewport.mEditContext);
            if (selected.Count == 0)
                return;

            var batchAction = viewport.mEditContext.BeginBatchAction();

            foreach (var point in selected)
                viewport.mEditContext.CommitAction(new UnitRailPointDeleteUndo(this, point));

            batchAction.Commit("Delete Rail Points");

            foreach (var point in selected)
                Points.Remove(point);

            CourseUnit.GenerateTileSubUnits();
        }

        public void OnKeyDown(LevelViewport viewport)
        {
            if (ImGui.IsKeyPressed(ImGuiKey.Delete))
                RemoveSelected(viewport);
            if (viewport.mEditContext.IsSelected(this) && ImGui.GetIO().KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.A))
                SelectAll(viewport.mEditContext);
        }

        public bool HitTest(LevelViewport viewport)
        {
            return LevelViewport.HitTestLineLoopPoint(GetPoints(viewport), 10f,
                    ImGui.GetMousePos());
        }

        public void OnMouseDown(LevelViewport viewport)
        {
            var ctx = viewport.mEditContext;
            bool isSelected = ctx.IsSelected(this);

            //Line hit test
            if (!isSelected && viewport.HoveredObject == this)
            {
                viewport.SelectBGUnit(this);
                isSelected = true;
            }

            if (!isSelected)
                return;

            mouseDownPos = viewport.ScreenToWorld(ImGui.GetMousePos());

            var selected = GetSelected(viewport.mEditContext);

            if (ImGui.GetIO().KeyAlt && selected.Count == 1)
            {
                var index = Points.IndexOf(selected[0]);
                //Insert and add
                Vector3 posVec = viewport.ScreenToWorld(ImGui.GetMousePos());
                Vector3 pos = new(
                     MathF.Round(posVec.X, MidpointRounding.AwayFromZero),
                     MathF.Round(posVec.Y, MidpointRounding.AwayFromZero),
                     selected[0].Position.Z);

                DeselectAll(viewport.mEditContext);

                if (Points.Count - 1 == index) //is last point
                    AddPoint(viewport, new RailPoint(pos));
                else
                    InsertPoint(viewport, new RailPoint(pos), index + 1);
            }
            else if (ImGui.GetIO().KeyAlt && selected.Count == 0) //Add new point from last 
            {
                //Insert and add
                Vector3 posVec = viewport.ScreenToWorld(ImGui.GetMousePos());
                Vector3 pos = new(
                     MathF.Round(posVec.X, MidpointRounding.AwayFromZero),
                     MathF.Round(posVec.Y, MidpointRounding.AwayFromZero),
                     2);

                DeselectAll(viewport.mEditContext);
                AddPoint(viewport, new RailPoint(pos));
            }
            else
            {
                if (!ImGui.GetIO().KeyCtrl && !ImGui.GetIO().KeyShift)
                    DeselectAll(viewport.mEditContext);
            }

            for (int i = 0; i < Points.Count; i++)
            {
                Vector3 point = Points[i].Position;

                var pos2D = viewport.WorldToScreen(new(point.X, point.Y, point.Z));
                Vector2 pnt = new(pos2D.X, pos2D.Y);
                bool isHovered = (ImGui.GetMousePos() - pnt).Length() < 6.0f;

                if (isHovered)
                    ctx.Select(Points[i]);

                Points[i].PreviousPosition = point;
            }
            mouseDown = true;
        }

        private Vector2[] GetPoints(LevelViewport viewport)
        {
            Vector2[] points = new Vector2[Points.Count];
            for (int i = 0; i < Points.Count; i++)
            {
                Vector3 p = Points[i].Position;
                points[i] = viewport.WorldToScreen(new(p.X, p.Y, p.Z));
            }
            return points;
        }

        public void OnMouseUp(LevelViewport viewport)
        {
            mouseDown = false;

            if (transformStart)
            {
                var batchAction = viewport.mEditContext.BeginBatchAction();

                foreach (var item in mTransformUndos)
                    viewport.mEditContext.CommitAction(item);

                batchAction.Commit($"{IconUtil.ICON_ARROWS_ALT} Move Rail Points");

                transformStart = false;
            }
        }

        private List<TransformUndo> mTransformUndos = [];

        public void OnSelecting(LevelViewport viewport)
        {
            if (!mouseDown)
                return;

            var ctx = viewport.mEditContext;

            Vector3 posVec = viewport.ScreenToWorld(ImGui.GetMousePos());
            Vector3 diff = posVec - mouseDownPos;
            if (diff.X != 0 && diff.Y != 0 && !transformStart)
            {
                transformStart = true;
                //Store each selected point for undoing
                mTransformUndos.Clear();
                foreach (var point in GetSelected(viewport.mEditContext))
                    mTransformUndos.Add(new TransformUndo(point.Transform));
            }

            bool anyTransformed = false;

            for (int i = 0; i < Points.Count; i++)
            {
                if (transformStart && ctx.IsSelected(Points[i]))
                {
                    anyTransformed = true;
                }
            }

            if (anyTransformed)
                CourseUnit.GenerateTileSubUnits();
        }

        public void Render(LevelViewport viewport, ImDrawListPtr mDrawList)
        {
            if (!Visible)
                return;

            var ctx = viewport.mEditContext;
            bool isSelected = viewport.mEditContext.IsSelected(this);

            if (ImGui.IsMouseClicked(0) && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                OnMouseDown(viewport);
            if (ImGui.IsMouseReleased(0))
                OnMouseUp(viewport);

            if (viewport.mEditorState == LevelViewport.EditorState.Selecting)
                OnSelecting(viewport);

            OnKeyDown(viewport);

            var lineThickness = viewport.HoveredObject == this ? 3.5f : 2.5f;

            for (int i = 0; i < Points.Count; i++)
            {
                Vector3 point = Points[i].Position;
                var pos2D = viewport.WorldToScreen(new(point.X, point.Y, point.Z));

                //Next pos 2D
                Vector2 nextPos2D = Vector2.Zero;
                if (i < Points.Count - 1) //is not last point
                {
                    nextPos2D = viewport.WorldToScreen(new(
                        Points[i + 1].Position.X,
                        Points[i + 1].Position.Y,
                        Points[i + 1].Position.Z));
                }
                else if (IsClosed) //last point to first if closed
                {
                    nextPos2D = viewport.WorldToScreen(new(
                       Points[0].Position.X,
                       Points[0].Position.Y,
                       Points[0].Position.Z));
                }
                else //last point but not closed, draw no line
                    continue;

                uint line_color = IsValidAngle(pos2D, nextPos2D) ? Color_Default : Color_SlopeError;
                if (isSelected && line_color != Color_SlopeError)
                    line_color = Color_SelectionEdit;

                mDrawList.AddLine(pos2D, nextPos2D, line_color, lineThickness);

                if (isSelected)
                {
                    //Arrow display
                    Vector3 next = i < Points.Count - 1 ? Points[i + 1].Position : Points[0].Position;
                    Vector3 dist = next - Points[i].Position;
                    var angleInRadian = MathF.Atan2(dist.Y, dist.X); //angle in radian
                    var rotation = Matrix4x4.CreateRotationZ(angleInRadian);

                    float width = 1f;

                    var line = Vector3.TransformNormal(new Vector3(0, width, 0), rotation);

                    Vector2[] arrow = new Vector2[2];
                    arrow[0] = viewport.WorldToScreen(Points[i].Position + dist / 2f);
                    arrow[1] = viewport.WorldToScreen(Points[i].Position + dist / 2f + line);

                    float alpha = 0.5f;

                    mDrawList.AddLine(arrow[0], arrow[1], ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, alpha)), lineThickness);
                }
            }

            if (isSelected)
            {
                for (int i = 0; i < Points.Count; i++)
                {
                    Vector3 point = Points[i].Position;
                    var pos2D = viewport.WorldToScreen(new(point.X, point.Y, point.Z));
                    Vector2 pnt = new(pos2D.X, pos2D.Y);

                    //Display point color
                    uint color = 0xFFFFFFFF;
                    if (Points[i].IsHovered || ctx.IsSelected(Points[i]))
                        color = ImGui.ColorConvertFloat4ToU32(new(0.84f, .437f, .437f, 1));

                    mDrawList.AddCircleFilled(pos2D, 6.0f, color);

                    bool isHovered = (ImGui.GetMousePos() - pnt).Length() < 6.0f;
                    Points[i].IsHovered = isHovered;
                    if (isHovered)
                        viewport.HoveredObject = Points[i];
                }
            }
        }

        private bool IsValidAngle(Vector2 point1, Vector2 point2)
        {
            var dist = point2 - point1;
            var angleInRadian = MathF.Atan2(dist.Y, dist.X); //angle in radian
            var angle = angleInRadian * (180.0f / (float)Math.PI); //to degrees

            //TODO improve check and simplify

            //The game supports 30 and 45 degree angle variants
            //Then ground (0) and wall (90)
            float[] validAngles = new float[]
            {
                0, -0,
                27, -27,
                45, -45,
                90, -90,
                135,-135,
                153,-153,
                180,-180,
            };

            return validAngles.Contains(MathF.Round(angle));
        }

        public class RailPoint
        {
            public Transform Transform = new Transform();

            public Vector3 Position
            {
                get { return Transform.Position; }
                set { Transform.Position = value; }
            }

            public bool IsHovered { get; set; }

            //For transforming
            public Vector3 PreviousPosition { get; set; }

            public RailPoint(Vector3 pos)
            {
                Position = pos;
            }

            public bool HitTest(LevelViewport viewport)
            {
                Vector3 point = Position;

                var pos2D = viewport.WorldToScreen(new(point.X, point.Y, point.Z));
                Vector2 pnt = new(pos2D.X, pos2D.Y);
                return (ImGui.GetMousePos() - pnt).Length() < 6.0f;
            }
        }
    }
}