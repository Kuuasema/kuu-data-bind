using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Kuuasema.Utils;
using System.Linq;

namespace Kuuasema.DataBinding.Editor {

    [CustomPropertyDrawer(typeof(ViewBindings))]
    public class ViewBindingsDrawer : PropertyDrawer {

        private static GUIStyle styleButtonCreate;
        private static GUIStyle styleButtonDelete;
        private static int indent = 0;

        private static void InitStyles() {
            if (styleButtonCreate == null) {
                styleButtonCreate = new GUIStyle(GUI.skin.button);
                styleButtonCreate.normal.textColor = Color.green;
            }
            if (styleButtonDelete == null) {
                styleButtonDelete = new GUIStyle(GUI.skin.button);
                styleButtonDelete.normal.textColor = Color.red;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            InitStyles();
            EditorGUI.BeginProperty(position, label, property);

            if (indent == 0) {
                Rect boxRect = position;
                boxRect.x -= 1;
                boxRect.y -= 1;
                boxRect.width += 2;
                boxRect.height += 2;
                GUI.Box(boxRect, "", EditorStyles.helpBox);
            }

            indent++;

            FieldInfo field = property.GetTargetField();
            if (field == null || field.GetCustomAttributes(typeof(SerializeReference), false).FirstOrDefault() == null) {
                EditorGUI.PropertyField(position, property, label, true);
            } else {
            
                Rect labelRect = position;
                labelRect.width = EditorGUIUtility.labelWidth;
                labelRect.height = 16;

                Rect buttonRect = position;
                buttonRect.x = position.x + position.width - 20;
                buttonRect.width = 20;
                buttonRect.height = 16;
                
                if (property.managedReferenceValue == null) {
                    labelRect.x += indent * 11;
                    GUI.Label(labelRect, label);
                    if (GUI.Button(buttonRect, "+", styleButtonCreate)) {
                        property.managedReferenceValue = Activator.CreateInstance(field.FieldType);
                    }
                } else {
                    if (GUI.Button(buttonRect, "X", styleButtonDelete)) {
                        if (EditorUtility.DisplayDialog("Delete Binding", $"Are you sure you want to delete the binding '{field.Name}'?'", "Yes", "No")) {
                            property.managedReferenceValue = null;
                        }
                    }
                    EditorGUI.PropertyField(position, property, label, true);
                }
            }

            indent--;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUI.GetPropertyHeight(property);
        }
    }
}
