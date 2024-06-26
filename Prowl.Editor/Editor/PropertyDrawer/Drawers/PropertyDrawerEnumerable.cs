﻿using Prowl.Editor.Preferences;
using Prowl.Icons;
using Prowl.Runtime;
using Prowl.Runtime.GUI;

namespace Prowl.Editor.PropertyDrawers
{
    public abstract class PropertyDrawerEnumerable<T> : PropertyDrawer where T : class
    {
        public static ulong selectedDrawer = 0;
        public static int selectedElement = -1;

        private static int windowBG = 0;
        private static Color bg => windowBG % 2 == 0 ? EditorStylePrefs.Instance.WindowBGOne : EditorStylePrefs.Instance.WindowBGTwo;

        protected abstract Type ElementType(T value);
        protected abstract int GetCount(T value);
        protected abstract object GetElement(T value, int index);
        protected abstract void SetElement(T value, int index, object element);
        protected abstract void RemoveElement(ref T value, int index);
        protected abstract void AddElement(ref T value);

        public override bool PropertyLayout(Gui gui, string label, int index, Type propertyType, ref object? propertyValue, EditorGUI.PropertyGridConfig config)
        {
            bool changed = false;

            T list = (T)propertyValue;


            using (gui.Node(label, index).ExpandWidth().FitContentHeight().Layout(LayoutType.Column).Enter())
            {
                gui.Draw2D.DrawRectFilled(gui.CurrentNode.LayoutData.Rect, EditorStylePrefs.Instance.WindowBGOne, (float)EditorStylePrefs.Instance.WindowRoundness);
                ulong drawerID = gui.CurrentNode.ID;

                if(drawerID == selectedDrawer)
                    selectedElement = MathD.Clamp(selectedElement, -1, GetCount(list));

                gui.TextNode("H_Text", label).ExpandWidth().Height(EditorStylePrefs.Instance.ItemSize).IgnoreLayout();

                bool enumexpanded = gui.GetNodeStorage<bool>("enumexpanded", false);
                using (gui.Node("EnumExpandBtn").TopLeft(5, 0).Scale(EditorStylePrefs.Instance.ItemSize).Enter())
                {
                    if (gui.IsNodePressed())
                    {
                        enumexpanded = !enumexpanded;
                        gui.SetNodeStorage(gui.CurrentNode.Parent, "enumexpanded", enumexpanded);
                    }
                    gui.Draw2D.DrawText(enumexpanded ? FontAwesome6.ChevronDown : FontAwesome6.ChevronRight, 20, gui.CurrentNode.LayoutData.InnerRect, gui.IsNodeHovered() ? EditorStylePrefs.Instance.LesserText : Color.white);
                }


                float scaleAnimContent = gui.AnimateBool(enumexpanded, 0.15f, EaseType.Linear);
                if (enumexpanded || scaleAnimContent > 0)
                {
                    using (gui.Node("_EnumDrawer").ExpandWidth().FitContentHeight(scaleAnimContent).Layout(LayoutType.Column).Spacing(5).Padding(10).Clip().Enter())
                    {

                        bool isPrimitive = ElementType(list).IsPrimitive || ElementType(list) == typeof(string);

                        for (int i = 0; i < GetCount(list); i++)
                        {
                            var element = GetElement(list, i);
                            using (gui.Node("_EnumElement", i).ExpandWidth().FitContentHeight().Enter())
                            {
                                if (drawerID == selectedDrawer && i == selectedElement)
                                    gui.Draw2D.DrawRectFilled(gui.CurrentNode.LayoutData.Rect, EditorStylePrefs.Instance.Highlighted);

                                if (gui.IsNodePressed())
                                {
                                    selectedDrawer = drawerID;
                                    selectedElement = i;
                                }
                                else if (gui.IsNodeHovered())
                                {
                                    gui.Draw2D.DrawRectFilled(gui.CurrentNode.LayoutData.Rect, EditorStylePrefs.Instance.Hovering);
                                }

                                changed |= DrawerAttribute.DrawProperty(gui, "Element " + i, i, ElementType(list), ref element, config);
                                if (changed)
                                    SetElement(list, i, element);
                            }
                        }

                        using (gui.Node("_Footer").ExpandWidth().Height(EditorStylePrefs.Instance.ItemSize).Layout(LayoutType.RowReversed).Enter())
                        {
                            gui.Draw2D.DrawRectFilled(gui.CurrentNode.LayoutData.Rect, EditorStylePrefs.Instance.WindowBGTwo, (float)EditorStylePrefs.Instance.WindowRoundness);

                            using (gui.Node("AddBtn").Scale(EditorStylePrefs.Instance.ItemSize).Enter())
                            {
                                if (gui.IsNodePressed())
                                {
                                    AddElement(ref list);
                                    changed = true;
                                }
                                else if (gui.IsNodeHovered())
                                {
                                    gui.Draw2D.DrawRectFilled(gui.CurrentNode.LayoutData.Rect, EditorStylePrefs.Instance.Hovering);
                                }
                                gui.Draw2D.DrawText(FontAwesome6.Plus, gui.CurrentNode.LayoutData.Rect);
                            }

                            using (gui.Node("RemoveBtn").Scale(EditorStylePrefs.Instance.ItemSize).Enter())
                            {
                                if (drawerID == selectedDrawer && selectedElement >= 0)
                                {
                                    if (gui.IsNodePressed())
                                    {
                                        RemoveElement(ref list, selectedElement);
                                        changed = true;
                                    }
                                    else if (gui.IsNodeHovered())
                                    {
                                        gui.Draw2D.DrawRectFilled(gui.CurrentNode.LayoutData.Rect, EditorStylePrefs.Instance.Hovering);
                                    }
                                }
                                gui.Draw2D.DrawText(FontAwesome6.Minus, gui.CurrentNode.LayoutData.Rect);
                            }

                            using (gui.Node("MoveDownBtn").Scale(EditorStylePrefs.Instance.ItemSize).Enter())
                            {
                                if (drawerID == selectedDrawer && selectedElement >= 0 && selectedElement < GetCount(list) - 1)
                                {
                                    if (gui.IsNodePressed())
                                    {
                                        var element = GetElement(list, selectedElement);
                                        var movedelement = GetElement(list, selectedElement + 1);
                                        SetElement(list, selectedElement, movedelement);
                                        SetElement(list, selectedElement + 1, element);
                                        selectedElement++;
                                        changed = true;
                                    }
                                    else if (gui.IsNodeHovered())
                                    {
                                        gui.Draw2D.DrawRectFilled(gui.CurrentNode.LayoutData.Rect, EditorStylePrefs.Instance.Hovering);
                                    }
                                    gui.Draw2D.DrawText(FontAwesome6.ArrowDown, gui.CurrentNode.LayoutData.Rect);
                                }
                                else
                                {
                                    gui.Draw2D.DrawText(FontAwesome6.ArrowDown, gui.CurrentNode.LayoutData.Rect, EditorStylePrefs.Instance.LesserText);
                                }
                            }

                            using (gui.Node("MoveUpBtn").Scale(EditorStylePrefs.Instance.ItemSize).Enter())
                            {
                                if (drawerID == selectedDrawer && selectedElement > 0)
                                {
                                    if (gui.IsNodePressed())
                                    {
                                        var element = GetElement(list, selectedElement);
                                        var movedelement = GetElement(list, selectedElement - 1);
                                        SetElement(list, selectedElement, movedelement);
                                        SetElement(list, selectedElement - 1, element);

                                        selectedElement--;
                                        changed = true;
                                    }
                                    else if (gui.IsNodeHovered())
                                    {
                                        gui.Draw2D.DrawRectFilled(gui.CurrentNode.LayoutData.Rect, EditorStylePrefs.Instance.Hovering);
                                    }
                                    gui.Draw2D.DrawText(FontAwesome6.ArrowUp, gui.CurrentNode.LayoutData.Rect);
                                }
                                else
                                {
                                    gui.Draw2D.DrawText(FontAwesome6.ArrowUp, gui.CurrentNode.LayoutData.Rect, EditorStylePrefs.Instance.LesserText);
                                }
                            }
                        }
                    }
                }
            }

            if(changed)
                propertyValue = list;

            return changed;
        }
    }


    [Drawer(typeof(Array))]
    public class PropertyDrawerArray : PropertyDrawerEnumerable<Array>
    {
        protected override Type ElementType(Array value) => value.GetType().GetElementType();
        protected override int GetCount(Array value) => value.Length;
        protected override object GetElement(Array value, int index) => value.GetValue(index);
        protected override void SetElement(Array value, int index, object element) => value.SetValue(element, index);
        protected override void RemoveElement(ref Array value, int index)
        {
            var elementType = value.GetType().GetElementType();
            var newArray = Array.CreateInstance(elementType, value.Length - 1);
            Array.Copy(value, 0, newArray, 0, index);
            Array.Copy(value, index + 1, newArray, index, value.Length - index - 1);
            value = newArray;
        }
        protected override void AddElement(ref Array value)
        {
            var elementType = value.GetType().GetElementType();
            var newArray = Array.CreateInstance(elementType, value.Length + 1);
            Array.Copy(value, newArray, value.Length);
            value = newArray;
        }
    }

    [Drawer(typeof(System.Collections.IList))]
    public class PropertyDrawerList : PropertyDrawerEnumerable<System.Collections.IList>
    {
        protected override Type ElementType(System.Collections.IList value) => value.GetType().GetGenericArguments()[0];
        protected override int GetCount(System.Collections.IList value) => value.Count;
        protected override object GetElement(System.Collections.IList value, int index) => value[index];
        protected override void SetElement(System.Collections.IList value, int index, object element) => value[index] = element;
        protected override void RemoveElement(ref System.Collections.IList value, int index) => value.RemoveAt(index);
        protected override void AddElement(ref System.Collections.IList value)
        {
            var elementType = value.GetType().GetGenericArguments()[0];
            var element = Activator.CreateInstance(elementType);
            value.Add(element);
        }
    }
}
