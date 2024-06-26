﻿using Prowl.Editor.Preferences;
using Prowl.Runtime;
using Prowl.Runtime.GUI;
using static Prowl.Runtime.GUI.Gui;

namespace Prowl.Editor.PropertyDrawers
{
    [Drawer(typeof(Transform))]
    public class Transform_PropertyDrawer : PropertyDrawer
    {
        public override bool OnValueGUI(Gui gui, string ID, Type targetType, ref object? targetValue)
        {
            gui.CurrentNode.Layout(LayoutType.Row).ScaleChildren();

            var value = (Transform)targetValue;

            bool changed = false;

            ActiveGUI.Draw2D.DrawRect(ActiveGUI.CurrentNode.LayoutData.Rect, EditorStylePrefs.Instance.Borders, 1, 2);

            var pos = ActiveGUI.CurrentNode.LayoutData.GlobalContentPosition;
            pos += new Vector2(0, 8);
            if (value == null)
            {
                string text = "(Null)" + targetType.Name;
                var col = EditorStylePrefs.Red * (ActiveGUI.IsNodeHovered() ? 1f : 0.8f);
                ActiveGUI.Draw2D.DrawText(text, pos, col);
            }
            else
            {
                ActiveGUI.Draw2D.DrawText(value.gameObject.Name + "(Transform)", pos, Color.white * (ActiveGUI.IsNodeHovered() ? 1f : 0.8f));
                if (ActiveGUI.IsNodeHovered() && ActiveGUI.IsPointerDoubleClick(Silk.NET.Input.MouseButton.Left))
                    GlobalSelectHandler.Select(value);
            }

            // Drag and drop support
            if (DragnDrop.Drop(out Transform instance))
            {
                targetValue = instance;
                changed = true;
            }

            // support looking for components on dropped GameObjects
            if (DragnDrop.Drop(out GameObject go))
            {
                targetValue = go.Transform;
                changed = true;
            }

            return changed;
        }
    }


}
