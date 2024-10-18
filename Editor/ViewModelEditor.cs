using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Kuuasema.DataBinding.Editor {
    [UnityEditor.CustomEditor(typeof(ViewModel), true)]
    public class ViewModelEditor : UnityEditor.Editor {

        public static bool DrawProperty { get; private set;}
        private static GUIContent guiContentBoundViews = new GUIContent("Bound Views");
        public override void OnInspectorGUI() {

            // this.DrawDefaultInspector();

            ViewModel viewModel = (ViewModel) target;
            DataModel dataModel = viewModel.GetDataModel();

            serializedObject.Update();

            bool isPlaying = Application.isPlaying;
            
		
			SerializedProperty prop = serializedObject.GetIterator();

            EditorGUILayout.BeginVertical();
			if (prop.NextVisible(true)) {
				do {
                    bool showData = false;
                    if (isPlaying && prop.propertyType == SerializedPropertyType.Generic && prop.isArray) {
                        if (prop.arrayElementType.StartsWith("PPtr<$ViewModel")) {
                            showData = true;
                        }
                    }
                    
                    if (showData) {
                        
                        

                        EditorGUILayout.BeginHorizontal();

                        string label = prop.displayName;
                        string key = prop.name;

                        prop.isExpanded = GUILayout.Toggle(prop.isExpanded, label, EditorStyles.foldout, GUILayout.Width(EditorGUIUtility.labelWidth));
                        
                        EditorGUILayout.LabelField("[D]", EditorStyles.boldLabel, GUILayout.MaxWidth(20));

                        DataModel _dataModel = dataModel?.Find(key);
                        if (_dataModel == null) {
                            GUILayout.Label("<NO DATA BOUND>");
                        } else {
                            this.DrawDataField(_dataModel);
                        }

                        EditorGUILayout.EndHorizontal();

                       

                        if (prop.isExpanded) {
                            EditorGUILayout.BeginVertical("Box");
                            DrawProperty = false;
                            EditorGUI.indentLevel++;


                            if (_dataModel != null) {
                                EditorGUI.indentLevel++;
                                EditorGUILayout.BeginVertical();
                                foreach (KeyValuePair<string,DataModel> keyVal in _dataModel.BindingMap) {
                                    EditorGUILayout.BeginHorizontal();
                                    GUILayout.Label("", GUILayout.Width(EditorGUIUtility.labelWidth / 2));
                                    GUILayout.Label(keyVal.Key, GUILayout.Width(EditorGUIUtility.labelWidth / 2));

                                    // GUILayout.Label("<ADD DATA FIELD>");
                                    this.DrawDataField(keyVal.Value);

                                    EditorGUILayout.EndHorizontal();
                                }
                                
                                EditorGUILayout.EndVertical();
                                EditorGUI.indentLevel--;
                            }

                            
                            EditorGUILayout.PropertyField(prop, guiContentBoundViews, true);
                            EditorGUI.indentLevel--;
                            DrawProperty = true;
                            EditorGUILayout.EndVertical();
                        }

                        

                    } else {
						EditorGUILayout.PropertyField(prop, true);
					}
				}
				while (prop.NextVisible(false));
			}
            EditorGUILayout.EndVertical();
		
			serializedObject.ApplyModifiedProperties();
        }     

        protected virtual void DrawDataField(DataModel baseModel) {

            switch (baseModel.DataType.Name) {
                case "Boolean": {
                    DataModel<bool> dataModel = baseModel as DataModel<bool>;
                    bool newValue = EditorGUILayout.Toggle(dataModel.Value);
                    if (!dataModel.IsEqual(newValue)) {
                        Debug.Log($"{dataModel.IsEquatable} :: {newValue} != {dataModel.Value}");
                        dataModel.TryChangeValue(newValue);
                    }
                } return;
                case "Single": {
                    DataModel<float> dataModel = baseModel as DataModel<float>;
                    float newValue = EditorGUILayout.FloatField(dataModel.Value);
                    if (!dataModel.IsEqual(newValue)) {
                        dataModel.TryChangeValue(newValue);
                    }
                } return;
                case "Int32": {
                    DataModel<int> dataModel = baseModel as DataModel<int>;
                    int newValue = EditorGUILayout.IntField(dataModel.Value);
                    if (!dataModel.IsEqual(newValue)) {
                        dataModel.TryChangeValue(newValue);
                    }
                } return;
                case "Vector3": {
                    DataModel<Vector3> dataModel = baseModel as DataModel<Vector3>;
                    // Vector3 newValue = EditorGUILayout.Vector3Field((GUIContent)null, dataModel.Value);
                    Vector3 newValue = EditorGUILayout.Vector3Field(GUIContent.none, dataModel.Value);
                    if (!dataModel.IsEqual(newValue)) {
                        dataModel.TryChangeValue(newValue);
                    }
                } return;
                default: {
                    if (typeof(Enum).IsAssignableFrom(baseModel.DataType)) {
                        Enum oldValue = (Enum)baseModel.GetValue();
                        Enum newValue = EditorGUILayout.EnumPopup((GUIContent)null, oldValue);
                        if (oldValue != newValue) {
                            baseModel.SetValue(newValue);
                        }
                        return;
                    }
                    GUILayout.Label($"{baseModel.DataType.Name}");
                } break;
            }

            // T newValue = this.DrawField(position, dataModel.Value);
            // if (!dataModel.IsEqual(newValue)) {
            //     dataModel.TryChangeValue(newValue);
            // }
        }   
    }


    public class ViewModelDrawer<T> : PropertyDrawer {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            ViewModel<T> viewModel = property.objectReferenceValue as ViewModel<T>;
            if (ViewModelEditor.DrawProperty && Application.isPlaying && viewModel != null) {
                
                position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
                DataModel<T> dataModel = viewModel.DataModel;
                if (dataModel == null) {
                    GUI.Label(position, "<NULL>");
                } else {
                    this.DrawDataField(position, viewModel, dataModel);
                }
            } else {
                EditorGUI.PropertyField(position, property, label, true);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUI.GetPropertyHeight(property);
        }

        protected virtual void DrawDataField(Rect position, ViewModel<T> viewModel, DataModel<T> dataModel) {
            T newValue = this.DrawField(position, dataModel.Value);
            if (!dataModel.IsEqual(newValue)) {
                dataModel.TryChangeValue(newValue);
            }
        }

        protected virtual T DrawField(Rect position, T oldValue) {
            return oldValue;
        }
    }

    [CustomPropertyDrawer(typeof(ViewModel<float>))]
    public class FloatViewModelDrawer : ViewModelDrawer<float> {

        protected override float DrawField(Rect position, float oldValue) {
            return EditorGUI.FloatField(position, oldValue);
        }
    }

}
