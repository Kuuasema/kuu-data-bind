/*
MIT License

Copyright (c) 2024 Kuuasema Ltd

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), 
to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Kuuasema.Utils;
namespace Kuuasema.DataBinding {
    /*
    * The main concept of this data binding system is that any object can be treated as data.
    * A data model is defined around that object, and that model include the fields of the object that were 
    * marked with the [DataBind] attribute. Defining the data model is trivial trough generics:
    *   new DataModel<MyDataClass>()
    * The datamodel class can further be defined, giving the possibility to assign shorthand properties to the data:
    *   class MyDataModel : DataModel<MyDataClass> {
    *     [PropertyBind]
    *     public DataModel<bool> MyState { get; set; }
    *   }
    * 
    * Each instance of data should have only one instance of data model. 
    * But what is unlimited on the other hand is ViewModels. In this system a ViewModel derives from MonoBehaviour,
    * and as such allows any kind of game component to serve as a representative of data. Most commonly you would 
    * consider such databinding as part of UI since that is where these patterns were invented, but a unity prefab, 
    * a character avatar is just as much capable of binding and representing data as is some UI component.
    * So how you define a view model is similar to how you defined the data model. Only this time it has to derive
    * from MonoBehaviour:
    *  class MyViewModel : ViewModel<MyDataClass> {}
    * As you can see the view model follows the same generics as the data model, and if those two are compatible
    * then they can be bound together. Any changes in the data model would be detected by the view model, as well
    * as the possibility for the viewmodel to update the data itself (possibly triggering other models to react to the change).
    *
    * Any number of view models can be bound to the same data model at any time. 
    */

    /**
    * [DataBind] attribute.
    * Add this to any field in any class and after that the class can function as data description for data binding.
    */
    public class DataBindAttribute : PropertyAttribute {
        // unique binding name in the context of the data object
        public string Name;
        public bool BindField;
        public DataBindAttribute() {}
        public DataBindAttribute(string name) {
            this.Name = name;
        }

        public DataBindAttribute(bool bindField) {
            this.BindField = bindField;
        }

        public DataBindAttribute(string name, bool bindField) {
            this.Name = name;
            this.BindField = bindField;
        }
    }

    /**
    * [CustomDataModel] attribute.
    * The data binding system will automatically find data models for data classes, 
    * but this attribute can be used to instruct to use a specific data model.
    */
    public class CustomDataModelAttribute : System.Attribute {
        public Type Type;
        public CustomDataModelAttribute(Type type) {
            this.Type = type;
        }
    }

    /**
    * [ViewBind] attribute.
    * The view bind attribute is used to bind into a data model. They are used only on classes that inherit from the ViewModel.
    * Marking a field in the viewmodel class with the attribute will bind the associated datamodel to it.
    * View models can use this attribute to bind into any other Component in the hierarchy, even if they are not view models.
    * In that case (when they are not view models) only the reference will be automatically resolved, no data binding will take place.
    */
    public class ViewBindAttribute : PropertyAttribute {
        // binding name to bind against
        public string Name;
        // optional function mapping for unity UI components 
        public string Target;
        public ViewBindAttribute() {
        }
        public ViewBindAttribute(string name) {
            this.Name = name;
        }
        public ViewBindAttribute(string name, string target) {
            this.Name = name;
            this.Target = target;
        }
    }

    /**
    * [PropertyBind] attribute.
    * The property binding is to be used on custom Data Model classes. It provides a more friendly access to the data model fields.
    * Without the property binding each data field needs to be accessed trough the Find(name) function.
    * What the property binding does, is that it fetches that data field once and assigns it into the property.
    */
    public class PropertyBindAttribute : System.Attribute {
        
    }

    /**
    * [PropertyInherit] attribute.
    * In some situations data models may want to access data in its parent context. For this the inherit attribute can be used.
    * The data filed will function just as with the normal property binding, but instead of expecting to find that data in the data object 
    * that it is bound to, it assigns the property to point to the data field in its data parent.
    */
    public class PropertyInheritAttribute : System.Attribute {
        
    }

    /**
    * [BindAction] attribute.
    * Often you want to write some custom code to be run when the value of a data field changes. 
    * For that you can subscribe to its data changed delegate. But also subscribing to it requires at least one line of code.
    * This attribute does that subscription, so you can mark a function that satisfies the delegate type, and let the system do the binding for you.
    */
    public class BindActionAttribute : System.Attribute {
        public string Target;
        public BindActionAttribute(string target) {
            this.Target = target;
        }
    }

    /**
    * [BindButton] attribute.
    * 
    */
    public class BindButtonAttribute : PropertyAttribute {
        // target mapping
        public string Target;
        public BindButtonAttribute(string target) {
            this.Target = target;
        }

        public MethodInfo Method { get; set; }
    }    

    /**
    * The static context map exists for more dynamic data entities to register themselves.
    * For example critical systems should register into here, for scripting and console access.
    * In addition if shorter lived gameplay agents register themselves here they can also be accessed as easily.
    */
    public static class DataContextMap {
        // TODO: if we introduce several maps for different type of objects we can speed the access.
        //       do this if the map is being heavily used each frame and starts to cost performance.
        public static Dictionary<string,DataModel> Map { get; private set; } = new Dictionary<string,DataModel>();

        public static void Register(string key, DataModel model) {
            Map[key] = model;
        }

        public static void Unregister(string key) {
            Map.Remove(key);
        }
        
        public static DataModel<T> Find<T>(string name) {
            if (DataContextMap.Map.TryGetValue(name, out DataModel dataModel)) {
                if (typeof(DataModel<T>).IsAssignableFrom(dataModel.GetType())) {
                    return (DataModel<T>) dataModel;
                }
            }
            return null;
        }
    }

    /**
    * This is the base data model class. It is intended that the derived generics class is to be used instead.
    * Its purpose is mainly to provide a non generic type to work with in the internal logic.
    */
    public class DataModel {
        //// STATIC ////
        // The template type for the generic data model class will be used to construct data models at runtime
        // With the templates we can construct generic pools for the data model instances.
        private static Type _TemplateType = typeof(DataModel<>);//Type.GetType("Kuuasema.DataBinding.DataModel`1");
        private static Type _PoolType = typeof(GenericPool<>);//Type.GetType("Kuuasema.Utils.GenericPool`1");
        private static Type _ListType = typeof(List<>);//Type.GetType("System.Collections.Generic.List`1");
        protected static Type TemplateType { get {
            if (_TemplateType == null) {
                _TemplateType = Type.GetType("Kuuasema.DataBinding.DataModel`1");
            }
            return _TemplateType;
        }} 
        protected static Type PoolType { get {
            if (_PoolType == null) {
                _PoolType = Type.GetType("Kuuasema.Utils.GenericPool`1");
            }
            return _PoolType;
        }} 
        protected static Type ListType { get {
            if (_ListType == null) {
                _ListType = Type.GetType("System.Collections.Generic.List`1");
            }
            return _ListType;
        }}
        
        // Static data model type, field and property mappings
        // Maps a data type to its data model type.
        protected static Dictionary<Type,Type> ModelTypeMap = new Dictionary<Type, Type>();
        // Stores the resolved generic pool type for each type.
        protected static Dictionary<Type,Type> PoolTypeMap = new Dictionary<Type, Type>();
        // Stores the generic list type for a each type.
        protected static Dictionary<Type,Type> ListTypeMap = new Dictionary<Type, Type>();
        // Stores the fields of each data model type.
        protected static Dictionary<Type,FieldInfo[]> FieldMap = new Dictionary<Type, FieldInfo[]>();
        // Stores the properties of each data model type.
        protected static Dictionary<Type,List<PropertyInfo>> PropertyMap = new Dictionary<Type, List<PropertyInfo>>();
        // Stores the inherited properties for each type.
        protected static Dictionary<Type,List<PropertyInfo>> PropertyInheritMap = new Dictionary<Type, List<PropertyInfo>>();
        // Stores the data binding fields and their attributes for each type.
        protected static Dictionary<Type,Dictionary<FieldInfo,DataBindAttribute>> TypeFieldAttributeMap = new Dictionary<Type,Dictionary<FieldInfo,DataBindAttribute>>();
        // Stores the cutom model mappings for types.
        protected static Dictionary<Type,Type> CustomModelTypeMap = new Dictionary<Type, Type>();
        // Maps data model methods with their data bind attributes.
        protected static Dictionary<Type,Dictionary<MethodInfo,BindActionAttribute>> TypeMethodAttributeMap = new Dictionary<Type,Dictionary<MethodInfo,BindActionAttribute>>();
        //
        protected static Dictionary<Type,List<MethodInfo>> MethodMap = new Dictionary<Type, List<MethodInfo>>();
        // Reusable argument array for reflection calls.
        protected static Type[] typeArgs = new Type[1];

        /**
        * Creates generic data model type for given data type.
        */
        protected static Type GetGenericType(Type dataType) {
            Type genericType = null;
            #if UNITY_EDITOR
            try {
            #endif
            if (!ModelTypeMap.TryGetValue(dataType, out genericType)) {
                typeArgs[0] = dataType;
                genericType = TemplateType.MakeGenericType(typeArgs);
                ModelTypeMap[dataType] = genericType;
            }
            #if UNITY_EDITOR
            } catch (System.Exception e) {
                // Debug.LogException(e);
                Debug.LogError($"Error creating type: DataModel<{dataType}> from {TemplateType}, {e.Message}");
            }
            #endif
            return genericType;
        }

        /**
        * Creates data model GenericPool type.
        */
        protected static Type GetGenericPool(Type datamodelType) {
            Type genericType;
            if (!PoolTypeMap.TryGetValue(datamodelType, out genericType)) {
                typeArgs[0] = datamodelType;
                // Debug.Log(datamodelType);
                genericType = PoolType.MakeGenericType(typeArgs);
                PoolTypeMap[datamodelType] = genericType;
            }
            return genericType;
        }

        /**
        * Creates data model List type.
        */
        protected static Type GetGenericList(Type elementType) {
            Type genericType;
            if (!ListTypeMap.TryGetValue(elementType, out genericType)) {
                typeArgs[0] = elementType;
                genericType = ListType.MakeGenericType(typeArgs);
                ListTypeMap[elementType] = genericType;
            }
            return genericType;
        }

        /**
        * Discover models is called as one of the very first things as the application starts (the InitializeStatic attibute).
        * It will find in the application assembly all the DataModel classes that exist and create the basic data type mapping.
        */
        [InitializeStatic(-1)]
        private static void DiscoverModels() {
            Type dataModelType = typeof(DataModel<>);
            Assembly dataAssembly = Assembly.GetAssembly(dataModelType);
            Assembly mainAssembly = Assembly.GetAssembly(typeof(UnityEngine.Object));
            Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in allAssemblies) {
                bool useAssembly = assembly == mainAssembly || assembly.FullName.Contains("Assembly-CSharp");
                if (!useAssembly) {
                    foreach (AssemblyName referencedAssembly in assembly.GetReferencedAssemblies()) {
                        if (referencedAssembly.FullName == dataAssembly.FullName) {
                            useAssembly = true;
                            break;
                        }
                    }
                }
                if (useAssembly) {
                    foreach (Type type in assembly.GetTypes()) {
                        // We can autuomatically extract custom models, but that gives less control
                        if (IsSubclassOfRawGeneric(dataModelType, type, out Type genericParameter)) {
                            ModelTypeMap[genericParameter] = type;
                        }
                    }
                    foreach (Type type in assembly.GetTypes()) {
                        // Get custom models from attributes
                        CustomDataModelAttribute attribute  = Attribute.GetCustomAttribute(type, typeof(CustomDataModelAttribute)) as CustomDataModelAttribute;
                        if (attribute != null) {
                            CustomModelTypeMap[attribute.Type] = type;
                            ModelTypeMap[attribute.Type] = type;
                        }
                    }
                }
            }
        }

        /**
        * Find the generic type that the given type is composed of.
        */
        private static bool IsSubclassOfRawGeneric(Type generic, Type toCheck, out Type genericParameter) {
            genericParameter = null;
            while (toCheck != null && toCheck != typeof(object)) {
                Type cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur) {
                    genericParameter = toCheck.GetGenericArguments()[0];
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //// INSTANCE ////
        // The following parts belongs to the data model instance.
        public Type DataType { get; protected set; }
        public virtual string ContextKey => null;

        // The binding map stores the data models with their binding names
        public Dictionary<string,DataModel> BindingMap { get; protected set; } = new Dictionary<string, DataModel>();
        protected Dictionary<string,Delegate> ActionMap { get; set; } = new Dictionary<string, Delegate>();

        /**
        * Returns the datamodel associated with the given binding key.
        */
        public DataModel Find(string name) {
            if (this.BindingMap == null || name == null) {
                Debug.LogError($"Binding Map or Name is null");
            }
            if (this.BindingMap.TryGetValue(name, out DataModel model)) {
                return model;
            }
            return null;
        }
        /**
        * Returns the datamodel associated with the given binding key, casting it to the desired type if possible.
        */
        public DataModel<T> Find<T>(string name) {
            if (this.BindingMap == null || name == null) {
                Debug.LogError($"Binding Map or Name is null");
            }
            if (this.BindingMap.TryGetValue(name, out DataModel model)) {
                if (typeof(DataModel<T>).IsAssignableFrom(model.GetType())) {
                    return model as DataModel<T>;
                }
            }
            return null;
        }
        
        // Concrete implementations of the following methods are in the in the generic class
        internal virtual void Bind(object data, DataModel parent) { }
        internal virtual void Bind<D>(D data, DataModel parent) { }
        internal virtual void BindInheritedProperties(DataModel parent) { }
        protected virtual void OnBound() {}
        public void Bind(object data) { 
            this.Bind(data, null);
        }
        
        internal virtual void BindField(FieldInfo field, object data) { }
        internal virtual void BindAction(Delegate action) {}
        internal virtual void UnBindAction(Delegate action) {}
        public virtual void Dispose() {}
        public virtual void StartListening(DataModel other) {}
        public virtual void StopListening() {}
        // instruct to reload the data model from the underlaying data in case it was modified directly outside from the data model
        public virtual void Reload() {}
        public virtual void Flush() {}
        public virtual void SetValue(object value) { }
        public virtual object GetValue() { return null; }
        public virtual void ParseValue(string value) { }
        // for collection support
        public virtual DataModel AddValue(object value) { return null; }
        public virtual void RemoveValue(object value) { }
        public virtual void SetValueAt(int index, object value) { }
        public virtual object GetValueAt(int index) { return null; }
        public virtual DataModel GetModelAt(int index) { return null; }
        
        
        // The data model can be locked to gain priviledged access to modifying it
        // this should not be abused, its intended for high frequency UI components like sliders to get exclusive control of the data
        public object LockingKey { get; private set; }

        /**
        * Locks the data model so that it can only be modified with the given key.
        */
        public virtual bool Lock(object key) {
            if (this.LockingKey != null) return false;
            this.LockingKey = key;
            return true;
        }

        /**
        * Unlocks the data model if the given key is correct.
        */
        public virtual bool Unlock(object key) {
            if (this.LockingKey == key) {
                this.LockingKey = null;
                return true;
            }
            return false;
        }
    }
    
    /**
    * This is the data model class to derive from and also use as such without deriving.
    */
    public class DataModel<T> : DataModel {

        //// STATIC ////
        private static object[] objectArgs = new object[1];
        

        //// FIELDS ////
        // the type of T
        // public Type DataType { get; private set; } <-- moved to base
        public bool IsEquatable { get; private set; }
        public bool IsEqual(T value) {
            if (!this.IsEquatable) {
                return false;
            }
            IEquatable<T> a = this.Value as IEquatable<T>;
            IEquatable<T> b = value as IEquatable<T>;
            return a.Equals(b);
        }
        // The data model can be set into Listening mode
        public bool IsListening { get; private set; } 
        private DataModel<T> listeningTo;
        private bool settingReference;
        
        
        //// PUBLIC INTERFACE ////
        // register this delegate to recieve data updates
        public delegate void ValueUpdate(T value);
        public ValueUpdate OnValueUpdated { get; set; }
        public T Value { get; private set; }

        /**
        * Sets the value in the model and notifies all observers.
        */
        public override void SetValue(object value) {
            // this abstraction was required so that data models of unknown generics can be operated with trough the non generic base
            // this.SetValue((T)value);
            this.TryChangeValue((T)value);
        }

        /**
        * Returns the value in the model.
        */
        public override object GetValue() {
            return this.Value;
        }

        /**
        * Attempts to parse the string and then assign that value.
        */
        public override void ParseValue(string value) {
            if (this.DataType.IsPrimitive) {
                this.TryChangeValue((T)Convert.ChangeType(value, this.DataType));
            }
        }

        public void SetValueWithAccess(T value, object accessKey) {
            this.SetValue(value, accessKey);
        }

        /**
        * Used internally to set the value.
        */
        private void SetValueConcrete(T value) {
            this.SetValue(value, null);
        }

        /**
        * This is the main set value method that we are actually going to call after all the type overrides
        */
        private void SetValue(T value, object accessKey = null) {
            // if this model is locked by someone else then who attempted to set the value now, then abort
            // anything can serve as a locking key, it can be any gameObject, component on it, some static data, or anyting else
            if (this.LockingKey != null && this.LockingKey != accessKey) return;
            // bind the assigned data value
            this.Bind(value, null);
            // notify observers
            if (this.OnValueUpdated != null) {
                this.OnValueUpdated(this.Value);
            }
            foreach (Delegate _action in this.boundActions) {
                if (_action is Action) {
                    // allow parameterless action binding (to just notify change, without changed value)
                    ((Action)_action)();
                } else {
                    Action<T> action = (Action<T>) _action;
                    action(value);
                }
            }

            // It this data model is set to listen to another model, and its value is changed, 
            // then change the value of what is being listened
            if (this.listeningTo != null && !this.settingReference) {
                this.settingReference = true;
                this.listeningTo.SetValue(value);
                this.settingReference = false;
            }
        }
        
        /**
        * This is a potentially more performant way to update the value in the data model.
        * This will first attempt to compare the new value with the old value, and if same then skip.
        * It works only if the data type is IEquatable.
        */
        public void TryChangeValue(T value) {
            if (!this.IsEquatable) {
                this.SetValue(value);
                return;
            }
            IEquatable<T> a = this.Value as IEquatable<T>;
            IEquatable<T> b = value as IEquatable<T>;
            if (a == null && b != null) {
                this.SetValue(value);
                return;
            }
             if (a != b) {
                this.SetValue(value);
            }
        }

        public void TryChangeValue(float value) {
            if (!Mathf.Approximately((float) (object) this.Value, value)) {
                this.SetValue(value);
            }
        }
        
        /**
        * The default constructor builds the model.
        */
        public DataModel() {
            this.BuildModel();
            this.OnModelBuilt();
        }

        /**
        * This is called when the model has been built.
        */
        protected virtual void OnModelBuilt() {

        }

        /**
        * The constructor overloaded with data will build the model and bind the given data to it.
        */
        public DataModel(T value) : this() {
            this.Value = value;
            this.Bind(this.Value, null);

            if (!string.IsNullOrWhiteSpace(this.ContextKey)) {
                DataContextMap.Register(this.ContextKey, this);
            }
        }

        //// COLLECTION SUPPORT ////
        // When using data models of type DataModel<List<>>, ie when the data is a list
        // we need some additional functions helping with accessing the elements

        public bool IsCollection { get; private set; }
        public bool IsList { get; private set; }
        public bool IsArray { get; private set; }
        public Type CollectionElementType { get; private set; }
        public Type CollectionModelType { get; private set; }
        public Type CollectionModelPoolType { get; private set; }
        public Type CollectionModelListType { get; private set; }
        public Type CollectionModelListPoolType { get; private set; }
        public IList ModelList { get; private set; }

        /**
        * Adding an element to the collection.
        */
        public delegate void ElementAdd(object value);
        public ElementAdd OnElementAdded { get; set; }
        public override DataModel AddValue(object value) { 
            if (this.IsList) {
                if (this.Value == null) {
                    Type _listType = GetGenericList(this.CollectionElementType);
                    Type _poolType = GetGenericPool(_listType);
                    this.Value = (T) _poolType.GetMethod("Get").Invoke(null, null);
                }
                IList list = this.Value as IList;
                list.Add(value);
                
                DataModel _model = this.CollectionModelPoolType.GetMethod("Get").Invoke(null, null) as DataModel;
                
                _model.Bind(value, this);
                this.ModelList.Add(_model);

                if (this.OnElementAdded != null) {
                    this.OnElementAdded(value);
                }
                return _model;
            } else if (this.DataType.IsPrimitive) {
                if (this.DataType == typeof(int)) {
                    this.SetValue((int)(object)this.Value + (int) value);
                } else if (this.DataType == typeof(float)) {
                    this.SetValue((float)(object)this.Value + (float) value);
                }
            }
            return null;
        }

        /**
        * Removing an element from the collection.
        */
        public delegate void ElementRemove(object value);
        public ElementRemove OnElementRemoved { get; set; }
        public override void RemoveValue(object value) {
            if (this.IsList) {
                IList list = this.Value as IList;
                int index = list.IndexOf(value);
                if (index > -1) {
                    list.Remove(value);
                    object _model = this.ModelList[index];
                    this.ModelList.RemoveAt(index);
                    objectArgs[0] = _model;
                    this.CollectionModelPoolType.GetMethod("Recycle").Invoke(null, objectArgs);
                    if (this.OnElementRemoved != null) {
                        this.OnElementRemoved(value);
                    }
                }
            } else if (this.DataType.IsPrimitive) {
                if (this.DataType == typeof(int)) {
                    this.SetValue((int)(object)this.Value - (int) value);
                } else if (this.DataType == typeof(float)) {
                    this.SetValue((float)(object)this.Value - (float) value);
                }
            }
        }

        /**
        * Updating an element in the collection.
        */
        public delegate void ElementUpdate(int index, object value);
        public ElementUpdate OnElementUpdated { get; set; }
        public override void SetValueAt(int index, object value) {
            if (this.IsList) {
                IList list = this.Value as IList;
                list[index] = value;
                if (this.OnElementUpdated != null) {
                    this.OnElementUpdated(index, value);
                }
            }
        }

        /**
        * Accessing an element in the collection.
        */
        public override object GetValueAt(int index) { 
            if (this.IsList) {
                IList list = this.Value as IList;
                return list[index];
            }
            return null;
        }

        /**
        * Accessing the bound data model for given element.
        */
        public override DataModel GetModelAt(int index) { 
            if (this.IsList) {
                return (DataModel) this.ModelList[index];
            }
            return null;
        }

        
        /**
        * Getting the collection as an enumerable.
        */
        public IEnumerable<K> GetEnumerable<K>() where K : DataModel {
            return this.ModelList as IEnumerable<K>;
        }
        
        //// PRIVATE ////
        // The following is the internal implementation of the data model.
        // TODO: operating with objects causes boxing to occur on primitives, investigate a way to bypass object parameters

        /**
        * Bind or unbinds data.
        */
        internal override void Bind(object data, DataModel parent) {
            if (data == null) {
                // when undinding we need to bind by default, which will be null for most
                // but not everything is nullable and can cause an error further down when the object is 
                // cast to its concrete type
                this.Bind(default(T), parent);
            } else {
                this.Bind((T)data, parent);
            }
        }

        /**
        * Bind data of concrete type.
        */
        internal override void Bind<D>(D data, DataModel parent) { 
            if (this.DataType.IsAssignableFrom(typeof(D))) {
                this.Bind((T)(object)data, parent);
            }
        }

        /**
        * Bind data of concrete type.
        * This is the main binding method that all the variants lead to.
        */
        protected virtual void Bind(T data, DataModel parent) {
            this.BindInternal(data, parent);
            this.BindInheritedProperties(parent);

            this.OnBound();
        }

        /**
        * The actual binding implementation.
        */
        private void BindInternal(T data, DataModel parent) {
            this.Value = data;
            if (!this.DataType.IsPrimitive) {
                // data is not primitive, ie. class or such then additional hierarchial actions are needed
                if (data == null) {
                    // data was nulled
                    foreach (Delegate _action in this.boundActions) {
                        Action<T> action = (Action<T>) _action;
                        action(default(T));
                    }

                    // unbind child model
                    foreach (DataModel model in this.BindingMap.Values) {
                        if (model != null) {
                            model.Bind(null, null);
                        }
                    }
                    // clear properties
                    foreach (PropertyInfo property in PropertyMap[this.GetType()]) {
                        if (property != null) {
                            property.SetValue(this, null);
                        }
                    }
                    // TODO: clear inherit properties

                } else {
                    // data object was assigned
                    // for each [DataBind] field, bind child data
                    foreach (KeyValuePair<FieldInfo,DataBindAttribute> fieldAttribute in TypeFieldAttributeMap[this.DataType]) {
                        FieldInfo field = fieldAttribute.Key;
                        DataBindAttribute attribute = fieldAttribute.Value;
                        if (attribute != null) {
                            // use binding map to find corresponding data model
                            // then bind the child value to it
                            if (this.BindingMap.TryGetValue(attribute.Name, out DataModel bindingModel)) {
                                object fieldValue = field.GetValue(data);
                                if (bindingModel.GetValue() != fieldValue) {
                                    bindingModel.Bind(fieldValue, this);
                                }
                                // bind field so that data model can modify undelaying data object
                                if (attribute.BindField) {
                                    this.BindingMap[attribute.Name].BindField(field, data);
                                }
                            }
                        }
                    }
                    if (this.IsList) {
                        // clear and recycle any previous models
                        this.RecycleListModels();
                        IList _list = this.Value as IList;
                        foreach (object _value in _list) {
                            object _model = this.CollectionModelPoolType.GetMethod("Get").Invoke(null, null);
                            (_model as DataModel).Bind(_value, this);
                            this.ModelList.Add(_model);
                        }
                    }
                    foreach (KeyValuePair<string,Delegate> keyVal in this.ActionMap) {
                        string bindingContext = keyVal.Key;
                        Delegate action = keyVal.Value;
                        DataModel _data = this.FindDataContext(bindingContext);
                        if (_data == null) {
                            Debug.LogError($"{this} {bindingContext}");
                        }
                        _data.BindAction(action);
                    }
                }
            }
            foreach (PropertyInfo property in PropertyMap[this.GetType()]) {
                if (this.BindingMap.TryGetValue(property.Name, out DataModel model)) {
                    Debug.Assert(model != null, $"Model \"{property.Name}\" not found!");
                    // This will throw error if you forget to add setter for the datamodel property
                    #if DEBUG_DATABINDING
                    try {
                        property.SetValue(this, model);
                    } catch (Exception e) {
                        Debug.LogException(e);
                        Debug.LogError($"Error assigning property \"{property.Name}\" to model of type {model.GetType().FullName} inside main model {this.GetType().FullName}");
                    }
                    #else
                    property.SetValue(this, model);
                    #endif
                }
            }

            // notify on binding
            if (this.OnValueUpdated != null) {
                this.OnValueUpdated(this.Value);
            }
        }

        protected DataModel FindDataContext(string path) {
            DataModel data = null;
            if (path.Contains(".")) {
                path = path.Replace(".", "/");
            }
            if (path.Contains("/")) {
                string[] parts = path.Split("/");
                data = this;
                for (int i = 0; i < parts.Length; i++) {
                    data = data.Find(parts[i]);
                }
            } else {
                data = this.Find(path);
            }
            return data;
        }

        /**
        * This binds the properties inherited from the parent.
        */
        internal override void BindInheritedProperties(DataModel parent) {
            if (parent != null && PropertyInheritMap.TryGetValue(this.GetType(), out List<PropertyInfo> inheritMap)) {
                foreach (PropertyInfo property in inheritMap) {
                    property.SetValue(this, parent.Find(property.Name));
                }
            }
            foreach (DataModel childModel in this.BindingMap.Values) {
                childModel.BindInheritedProperties(this);
            }
        }

        /**
        * Binds the actual underlaying data field to recieve updates from the data model.
        * This may not be neccessary for all use cases, and could be turned to an opt-in thing to save some resources.
        * This is neccessary if operating on persited objects that need to be serialized later.
        */
        internal override void BindField(FieldInfo field, object data) {
            this.OnValueUpdated += (value) => { field.SetValue(data, value); };
        }


        //// ACTION BINDING ////
        
        private List<Delegate> boundActions = new List<Delegate>();

        /**
        * Binds action to the data model.
        */
        internal override void BindAction(Delegate _action) {
            this.boundActions.Add(_action);
        }

        /**
        * Unbinds action from the data model.
        */
        internal override void UnBindAction(Delegate _action) {
            this.boundActions.Remove(_action);
        }

        /**
        * This constructs the data model.
        */
        protected virtual void BuildModel() {
            // store the data type
            Type type = typeof(T);
            this.DataType = type;

            // mark if the data is equatable
            if (typeof(IEquatable<T>).IsAssignableFrom(type)) {
                this.IsEquatable = true;
            }

            // if the data type is a collection, mark that and resolve the types that are needed
            if (typeof(IEnumerable).IsAssignableFrom(type)) {
                this.IsCollection = true;
                if (typeof(IList).IsAssignableFrom(type)) {
                    this.IsList = true;
                    this.CollectionElementType = type.GetGenericArguments()[0];
                    this.CollectionModelType = GetGenericType(this.CollectionElementType);
                    this.CollectionModelPoolType = GetGenericPool(this.CollectionModelType);
                    this.CollectionModelListType = GetGenericList(this.CollectionModelType);
                    this.CollectionModelListPoolType = GetGenericPool(this.CollectionModelListType);
                    object _list = this.CollectionModelListPoolType.GetMethod("Get").Invoke(null, null);
                    this.ModelList = (IList) _list;
                    
                } else {
                    this.IsArray = true;
                    this.CollectionElementType = type.GetElementType();
                }
            }

            // get data bind attributes on fields and properties
            FieldInfo[] fields;
            Type modelType = this.GetType();
            List<PropertyInfo> modelProperties;
            List<PropertyInfo> modelInheritProperties;

            // build the static model mappings if neccessary
            Dictionary<FieldInfo,DataBindAttribute> fieldAttributeMap;
            if (!TypeFieldAttributeMap.TryGetValue(type, out fieldAttributeMap)) {
                fieldAttributeMap = new Dictionary<FieldInfo, DataBindAttribute>();
                TypeFieldAttributeMap[type] = fieldAttributeMap;
            }

            // build field mapping if neccessary
            if (!FieldMap.TryGetValue(type, out fields)) {
                fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                FieldMap[type] = fields;
            }

            // build property mapping if neccessary
            if (!PropertyMap.TryGetValue(modelType, out modelProperties)) {
                modelProperties = new List<PropertyInfo>();
                PropertyInfo[] properties = modelType.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                foreach (PropertyInfo property in properties) {
                    // normal property binding
                    PropertyBindAttribute attribute = Attribute.GetCustomAttribute(property, typeof(PropertyBindAttribute)) as PropertyBindAttribute;
                    if (attribute != null) {
                        modelProperties.Add(property);
                    } else {
                        // property inheritance
                        PropertyInheritAttribute inheritAttribute = Attribute.GetCustomAttribute(property, typeof(PropertyInheritAttribute)) as PropertyInheritAttribute;
                        if (inheritAttribute != null) {
                            if (!PropertyInheritMap.TryGetValue(modelType, out modelInheritProperties)) {
                                modelInheritProperties = new List<PropertyInfo>();
                                PropertyInheritMap[modelType] = modelInheritProperties;
                            }
                            modelInheritProperties.Add(property);
                        }
                    }
                }
                PropertyMap[modelType] = modelProperties;
            } 

            // create method attribute mapping for given type
            Dictionary<MethodInfo,BindActionAttribute> methodAttributeMap;
            if (!TypeMethodAttributeMap.TryGetValue(modelType, out methodAttributeMap)) {
                methodAttributeMap = new Dictionary<MethodInfo, BindActionAttribute>();
                TypeMethodAttributeMap[modelType] = methodAttributeMap;
            }

            // create method mapping for given type
            List<MethodInfo> methods;
            if (!MethodMap.TryGetValue(modelType, out methods)) {
                MethodInfo[] _methods = modelType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                methods = new List<MethodInfo>();
                // filter result so that it only includes these with the attribute
                // so that the following for loop will be leaner
                // else we end up checking non bound fields over and over again
                foreach (MethodInfo method in _methods) {
                    // get the field attribute from the static map or populate it
                    BindActionAttribute attribute = Attribute.GetCustomAttribute(method, typeof(BindActionAttribute)) as BindActionAttribute;
                    if (attribute != null) { 
                        methodAttributeMap[method] = attribute;
                        methods.Add(method);
                    }
                }
                MethodMap[modelType] = methods;
            }

            // process each databind field 
            for (int i = 0; i < fields.Length; i++) {
                FieldInfo field = fields[i];
                DataBindAttribute attribute;
                // establish the static field attribute mapping for this type if neccessary
                if (!fieldAttributeMap.TryGetValue(field, out attribute)) {
                    attribute = Attribute.GetCustomAttribute(field, typeof(DataBindAttribute)) as DataBindAttribute;
                    if (attribute != null) {
                        if (string.IsNullOrWhiteSpace(attribute.Name)) {
                            attribute.Name = field.Name;
                        }
                    }
                }
                if (attribute != null) {
                    fieldAttributeMap[field] = attribute;
                    // if attribute exists for the field then instantiate data model for it
                    // store the created data model in the binding map
                    Type _modelType = GetGenericType(field.FieldType);
                    if (RuntimeUtils.IsRunning) {
                        // use pools at runtime
                        Type _poolType = GetGenericPool(_modelType);
                        object model = _poolType.GetMethod("Get").Invoke(null, null);
                        this.BindingMap[attribute.Name] = model as DataModel;
                    } else {
                        objectArgs[0] = null;
                        this.BindingMap[attribute.Name] = Activator.CreateInstance(_modelType, objectArgs) as DataModel;
                    }
                }
            }

            // for each action target, bind them
            for (int i = 0; i < methods.Count; i++) {
                MethodInfo method = methods[i];
                BindActionAttribute attribute = methodAttributeMap[method];
                this.BindActionTarget(attribute.Target, method);
            }
        }

        /**
        * 
        */
        protected void BindActionTarget(string target, MethodInfo method) {
            this.ActionMap[target] = this.CreateDelegate(method);
        }
        
        /**
        * 
        */
        private Delegate CreateDelegate(MethodInfo method) {
            if (method == null) {
                throw new ArgumentNullException("method");
            }
            if (method.IsGenericMethod) {
                throw new ArgumentException("The provided method must not be generic.", "method");
            }
            typeArgs[0] = typeof(void);
            return method.CreateDelegate(Expression.GetDelegateType(
                (from parameter in method.GetParameters() select parameter.ParameterType)
                .Concat(typeArgs)
                .ToArray()), this);
        }
        
        /**
        * Start listening to other data model.
        */
        public override void StartListening(DataModel other) {
            if (this.listeningTo == other) {
                return;
            }
            Type modelType = this.GetType();
            if (modelType.IsAssignableFrom(other.GetType())) {
                this.StartListening((DataModel<T>) other);
            }
        }

        /**
        * Start listening to other data model of generic type.
        */
        private void StartListening(DataModel<T> other) {
            if (this.listeningTo == other) {
                return;
            }
            this.StopListening();
            this.TryChangeValue(other.Value);
            this.listeningTo = other;
            this.listeningTo.OnValueUpdated += this.SetValueConcrete;

            foreach (KeyValuePair<string,DataModel> keyVal in this.BindingMap) {
                string key = keyVal.Key;
                DataModel model = keyVal.Value;
                model.StartListening(other.Find(key));
            }
            this.IsListening = true;
        }

        /**
        * Stop listening to other data model.
        */
        public override void StopListening() {
            if (this.listeningTo != null) {
                foreach (DataModel model in this.BindingMap.Values) {
                    model.StopListening();
                }
                this.listeningTo.OnValueUpdated -= this.SetValueConcrete;
                this.listeningTo = null;
            }
            this.IsListening = false;
        }

        /**
        * Reloads the data model from the underlaying data.
        */
        public override void Reload() {
            foreach (KeyValuePair<FieldInfo,DataBindAttribute> fieldAttribute in TypeFieldAttributeMap[this.DataType]) {
                FieldInfo field = fieldAttribute.Key;
                DataBindAttribute attribute = fieldAttribute.Value;
                if (attribute != null) {
                    this.BindingMap[attribute.Name].SetValue(field.GetValue(this.Value));
                }
            }
        }

        /**
        * Flushes the data model into the underlaying data.
        */
        public override void Flush() {
            foreach (KeyValuePair<FieldInfo,DataBindAttribute> fieldAttribute in TypeFieldAttributeMap[this.DataType]) {
                FieldInfo field = fieldAttribute.Key;
                DataBindAttribute attribute = fieldAttribute.Value;
                if (attribute != null) {
                    field.SetValue(this.Value, this.BindingMap[attribute.Name].GetValue());
                }
            }
        }

        /**
        * Dispose the data model, freeing and recucling resources.
        */        
        public override void Dispose() {
            foreach (DataModel model in this.BindingMap.Values) {
                model.Dispose();
            }
            this.BindingMap.Clear();
            this.OnValueUpdated = null;
            this.boundActions.Clear();
            this.Value = default(T);
            if (RuntimeUtils.IsRunning) {
                // use pools at runtime
                Type _poolType = GetGenericPool(this.GetType());
                objectArgs[0] = this;
                _poolType.GetMethod("Recycle").Invoke(null, objectArgs);

                if (this.IsList && this.ModelList != null) {
                    this.RecycleListModels();
                    objectArgs[0] = this.ModelList;
                    this.CollectionModelListPoolType.GetMethod("Recycle").Invoke(null, objectArgs);
                    this.ModelList = null;
                }
            }
        }

        /**
        * Recycles the models in the collection.
        */  
        private void RecycleListModels() {
            if (typeof(DataModel).IsAssignableFrom(this.CollectionElementType)) {
                foreach (DataModel _model in this.ModelList) {
                    if (_model != null) {
                        _model.Dispose();
                    }
                }
            }
            this.ModelList.Clear();
        }
    }

    // THIS PROBABLY WONT WORK EASILY SINCE VIEW MODELS NEED TO LISTEN TO CONCRETE DATA MODELS
    // NEED TO MAKE ALL BINDINGS GARBAGE FREE AS MUCH AS POSSIBLE
    // /**
    // * Solution for listening to any compatible DataModel<T> in an efficient manner.
    // * Ideally when using this, even while frequently changing the listening target, it should not produce any garbage.
    // */ 
    // public class DataListener<T> {
    //     public DataModel<T> ListenTarget { get; private set; }
    //     public virtual void StartListening(DataModel<T> other) {
    //     }
    //     public virtual void StopListening() {
    //     }
    // }

    /**
    * This is the base view model class. It is intended that the derived generics class is to be used instead.
    */
    public class ViewModel : MonoBehaviour {
        //// STATIC ////
        // static type mappings

        /**
        * Maps data type to view model tyope.
        */
        protected static Dictionary<Type,Type> ModelTypeMap = new Dictionary<Type, Type>();
        /**
        * Maps view model type to field list.
        */
        protected static Dictionary<Type,List<FieldInfo>> FieldMap = new Dictionary<Type, List<FieldInfo>>();
        /**
        * Maps view model type to method list.
        */
        protected static Dictionary<Type,List<MethodInfo>> MethodMap = new Dictionary<Type, List<MethodInfo>>();
        /**
        * Maps view model fields with their view bind attributes.
        */
        protected static Dictionary<Type,Dictionary<FieldInfo,ViewBindAttribute>> TypeFieldAttributeMap = new Dictionary<Type,Dictionary<FieldInfo,ViewBindAttribute>>();
        /**
        * Maps view model methods with their view bind attributes.
        */
        protected static Dictionary<Type,Dictionary<MethodInfo,BindActionAttribute>> TypeMethodAttributeMap = new Dictionary<Type,Dictionary<MethodInfo,BindActionAttribute>>();
        /**
        * Maps buttons to actions.
        */
        protected static Dictionary<Type,Dictionary<FieldInfo,BindButtonAttribute>> TypeButtonActionMap = new Dictionary<Type,Dictionary<FieldInfo,BindButtonAttribute>>();
        
        /**
        * Returns the field attribute map for the view model type.
        */
        public static Dictionary<FieldInfo,ViewBindAttribute> GetFieldAttributeMap(Type type) {
            Dictionary<FieldInfo,ViewBindAttribute> map = null;
            TypeFieldAttributeMap.TryGetValue(type, out map);
            return map;
        }

        // reusable array for reflection
        private static Type[] typeArgs = new Type[1];
        
        //// INSTANCE ////

        // binding map to view models
        protected Dictionary<string,List<ViewModel>> BindingMap { get; set; } = new Dictionary<string, List<ViewModel>>();
        // binding map to components
        protected Dictionary<string,Component> ComponentMap { get; set; } = new Dictionary<string, Component>();
        public Component MainComponent { get; private set; }
        protected Dictionary<string,Delegate> ActionMap { get; set; } = new Dictionary<string, Delegate>();
        

        // the binding context is set in the generics class
        public virtual string BindingContext => null;

        /**
        * On awake, build the view.
        */
        protected virtual void Awake() {
            if (!this.IsBuilt) this.BuildView();
        }

        /**
        * On destroy, recycle the binding lists.
        */
        protected virtual void OnDestroy() {
            foreach (List<ViewModel> boundViews in this.BindingMap.Values) {
                boundViews.Clear();
                GenericPool<List<ViewModel>>.Recycle(boundViews);
            }
            this.ComponentMap.Clear();
            this.ActionMap.Clear();
        }

        // viewmodels keep track of their parent view
        public ViewModel ParentView { get; private set; }
        /**
        * Sets the parent view model for this view model.
        */
        internal void SetParentView(ViewModel parent) {
            this.ParentView = parent;
        }

        // the data model is returned in the generic class
        public virtual DataModel GetDataModel() { return null; }

        /**
        * Find child view model based on binding name
        */
        public static ViewModel Find(ViewModel context, string name) {
            foreach (ViewModel child in context.GetComponentsInChildren<ViewModel>()) {
                if (child.BindingContext == name) {
                    return child;
                }
            }
            return null;
        }

        /**
        * Create data can be called on a view model that has been instantated but has no data to bind.
        * This will create the default data of the type and bind that.
        */
        public virtual void CreateData() {}
        public virtual void NotifyAll() {}
        
        public virtual Type DataType => this.GetType();
        public virtual Type ModelType => typeof(DataModel);

        /**
        * Binds the given data model to this view model.
        */
        public virtual void BindData(DataModel dataModel) { 
            if (!this.IsBuilt) this.BuildView();
        }
        
        /**
        * Unbinds currently bound data model.
        */
        public virtual void UnBindData() { }

        public bool IsBuilt { get; private set; }

        /**
        * Building the view.
        */
        public void BuildView() {
            if (this.IsBuilt) return;
            this.IsBuilt = true;

            Type type = this.GetType();
            List<FieldInfo> fields;
            List<MethodInfo> methods;

            // setup static mappings

            Dictionary<FieldInfo,ViewBindAttribute> fieldAttributeMap;
            Dictionary<MethodInfo,BindActionAttribute> methodAttributeMap;
            Dictionary<FieldInfo,BindButtonAttribute> buttonActionMap;
            
            bool populateFieldAttributeMap = false;
            bool populateMethodAttributeMap = false;
            bool populateButtonActionMap = false;

            // create field attribute map for given type
            if (!TypeFieldAttributeMap.TryGetValue(type, out fieldAttributeMap)) {
                fieldAttributeMap = new Dictionary<FieldInfo, ViewBindAttribute>();
                TypeFieldAttributeMap[type] = fieldAttributeMap;
                populateFieldAttributeMap = true;
            }
            // create method attribute mapping for given type
            if (!TypeMethodAttributeMap.TryGetValue(type, out methodAttributeMap)) {
                methodAttributeMap = new Dictionary<MethodInfo, BindActionAttribute>();
                TypeMethodAttributeMap[type] = methodAttributeMap;
                populateMethodAttributeMap = true;
            }
            // create button action mapping for given type
            if (!TypeButtonActionMap.TryGetValue(type, out buttonActionMap)) {
                buttonActionMap = new Dictionary<FieldInfo, BindButtonAttribute>();
                TypeButtonActionMap[type] = buttonActionMap;
                populateButtonActionMap = true;
            }

            

            // create field mapping for given type
            if (!FieldMap.TryGetValue(type, out fields)) {
                FieldInfo[] _fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                fields = new List<FieldInfo>();
                // filter result so that it only includes these with the attribute
                // so that the following for loop will be leaner
                // else we end up checking non bound fields over and over again
                foreach (FieldInfo field in _fields) {
                    // get the field attribute from the static map or populate it
                    ViewBindAttribute viewBindAttribute;
                    if (populateFieldAttributeMap) {
                        viewBindAttribute = Attribute.GetCustomAttribute(field, typeof(ViewBindAttribute)) as ViewBindAttribute;
                        if (viewBindAttribute != null) { 
                            fieldAttributeMap[field] = viewBindAttribute;
                        }
                    } else {
                        fieldAttributeMap.TryGetValue(field, out viewBindAttribute);
                    }
                    if (viewBindAttribute != null) {
                        // has attribute, store in field list
                        fields.Add(field);
                    }

                    // get the field attribute from the static map or populate it
                    BindButtonAttribute bindButtonAttribute;
                    if (populateButtonActionMap) {
                        bindButtonAttribute = Attribute.GetCustomAttribute(field, typeof(BindButtonAttribute)) as BindButtonAttribute;
                        if (bindButtonAttribute != null) { 
                            buttonActionMap[field] = bindButtonAttribute;
                        }
                    } else {
                        buttonActionMap.TryGetValue(field, out bindButtonAttribute);
                    }
                    if (bindButtonAttribute != null) {
                    
                        MethodInfo method = type.GetMethod(bindButtonAttribute.Target, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                        if (method != null) {
                            // has attribute and target method, store in field list
                            fields.Add(field);
                            bindButtonAttribute.Method = method;
                        }
                    }
                }
                FieldMap[type] = fields;
            }

            // create method mapping for given type
            if (!MethodMap.TryGetValue(type, out methods)) {
                MethodInfo[] _methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                methods = new List<MethodInfo>();
                // filter result so that it only includes these with the attribute
                // so that the following for loop will be leaner
                // else we end up checking non bound fields over and over again
                foreach (MethodInfo method in _methods) {
                    // get the field attribute from the static map or populate it
                    BindActionAttribute attribute;
                    if (populateMethodAttributeMap) {
                        attribute = Attribute.GetCustomAttribute(method, typeof(BindActionAttribute)) as BindActionAttribute;
                        if (attribute != null) { 
                            methodAttributeMap[method] = attribute;
                        }
                    } else {
                        methodAttributeMap.TryGetValue(method, out attribute);
                    }
                    if (attribute != null) {
                        // has attribute, store in field list
                        methods.Add(method);
                    }
                }
                MethodMap[type] = methods;
            }

            // all mappings have been created
            // iterate trough the fields and for each view model, set parent to this and build its view
            for (int i = 0; i < fields.Count; i++) {
                FieldInfo field = fields[i];

                // we know that [ViewBind] attribute nust be found on field, else it wouldnt been added to the list
                if (fieldAttributeMap.TryGetValue(field, out ViewBindAttribute viewBindAttribute)) {
                    ViewModel viewModel;

                    bool isList = typeof(IList).IsAssignableFrom(field.FieldType);
                    bool isViewModel = !isList && typeof(ViewModel).IsAssignableFrom(field.FieldType);

                    if (!isList && !isViewModel) {

                        if (viewBindAttribute.Name == "TestButton") {
                            Debug.Log("break");
                        }

                        // field is not a view model, so try find the component on it
                        Component component = field.GetValue(this) as Component;
                        if (component == null) {
                            component = FindComponent(this.transform, field.FieldType, viewBindAttribute.Name);
                            field.SetValue(this, component);
                        }
                        // derive target from attribute or field name if missing
                        if (string.IsNullOrWhiteSpace(viewBindAttribute.Target)) {
                            if (!string.IsNullOrWhiteSpace(viewBindAttribute.Name)) {
                                viewBindAttribute.Target = viewBindAttribute.Name;
                            } else {
                                viewBindAttribute.Target = field.Name;
                            }
                        }
                        if (component != null && !string.IsNullOrWhiteSpace(viewBindAttribute.Target)) {
                            // bind component
                            this.BindComponentTarget(viewBindAttribute.Target, component);
                        }
                    } else {

                        // if the attribute name is empty, inherit from the field name
                        if (string.IsNullOrWhiteSpace(viewBindAttribute.Name)) {
                            viewBindAttribute.Name = field.Name;
                        }

                        // get or create the list of bound views for this attribute
                        List<ViewModel> boundViews;
                        if (!this.BindingMap.TryGetValue(viewBindAttribute.Name, out boundViews)) {
                            boundViews = GenericPool<List<ViewModel>>.Get();
                            this.BindingMap[viewBindAttribute.Name] = boundViews;
                        }

                        // the field is a list of view models assigned trough the inspector
                        if (isList) {
                            IList list = field.GetValue(this) as IList;
                            foreach (object obj in list) {
                                viewModel = obj as ViewModel;
                                if (viewModel != null) {
                                    viewModel.SetParentView(this);
                                    viewModel.BuildView();
                                    boundViews.Add(viewModel);
                                }
                            }
                        }
                        // the field is a view model, it can be assigned, if not then discover it
                        else if (isViewModel) {
                            viewModel = field.GetValue(this) as ViewModel;
                            if (viewModel == null) {
                                // allow inspector assigning, but revert to discovery
                                viewModel = FindComponent(this.transform, field.FieldType, viewBindAttribute.Name) as ViewModel;
                                if (viewModel == null) {
                                    // component could be on this game object
                                    // if this code sticks around and remains used, move it into the FindComponent method
                                    viewModel = this.GetComponent(field.FieldType) as ViewModel;
                                }
                                if (viewModel != null) {
                                    // component was found, assign it to the blank field
                                    field.SetValue(this, viewModel);
                                } else {
                                    Debug.LogWarning($"Could not bind view: {field.Name}", this);
                                    continue;
                                }
                            }
                            // build the view and store in the binding map
                            viewModel.SetParentView(this);
                            viewModel.BuildView();
                            boundViews.Add(viewModel);   
                        }
                    }
                } else if (buttonActionMap.TryGetValue(field, out BindButtonAttribute bindButtonAttribute)) {

                    // field is (should only be) a button
                    Button button = field.GetValue(this) as Button;
                    if (button == null) {
                        button = FindComponent(this.transform, field.FieldType, field.Name) as Button;
                        field.SetValue(this, button);
                    }

                    button.onClick.AddListener(() => { ((Action)this.CreateDelegate(bindButtonAttribute.Method))(); });
                }
            }

            // for each action target, bind them
            for (int i = 0; i < methods.Count; i++) {
                MethodInfo method = methods[i];
                BindActionAttribute attribute = methodAttributeMap[method];
                this.BindActionTarget(attribute.Target, method);
            }
        }

        /**
        * Find the object using the transform.Find and get the component.
        */
        private static Component FindComponent(Transform context, Type componentType, string name) {
            Transform target;
            if (string.IsNullOrWhiteSpace(name)) {
                target = context;
            } else {
                target = context.Find(name);
            }
            if (target == null) return null;
            Component component = target.GetComponent(componentType);
            return component;
        }

        /**
        * 
        */
        protected virtual void BindComponentTarget(string target, Component component) {
            this.ComponentMap[target] = component;
            if (this.DataType.IsAssignableFrom(component.GetType())) {
                this.MainComponent = component;
            }
        }

        /**
        * 
        */
        protected void BindActionTarget(string target, MethodInfo method) {
            this.ActionMap[target] = this.CreateDelegate(method);
        }
        
        /**
        * 
        */
        private Delegate CreateDelegate(MethodInfo method) {
            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            // if (!method.IsStatic)
            // {
            //     throw new ArgumentException("The provided method must be static.", "method");
            // }

            if (method.IsGenericMethod)
            {
                throw new ArgumentException("The provided method must not be generic.", "method");
            }

            typeArgs[0] = typeof(void);

            return method.CreateDelegate(Expression.GetDelegateType(
                (from parameter in method.GetParameters() select parameter.ParameterType)
                .Concat(typeArgs)
                .ToArray()), this);
        }
    }

    /**
    * This is the view model class to derive from.
    */
    public class ViewModel<T> : ViewModel<T,DataModel<T>> {}
    public class ViewModel<T,K> : ViewModel where K : DataModel<T> {

        public override string BindingContext => nameof(T);
        public K DataModel { get; private set; }
        public override Type ModelType => typeof(K);
        public override DataModel GetDataModel() { return this.DataModel; }
        public override Type DataType => typeof(T);
        // private static object[] objectArgs = new object[1];
        protected override void Awake() {
            base.Awake();
        }
        protected virtual bool CreateDataOnStart => false;
        protected virtual void Start() {
            // if data is not bound so far, then check for automatic data creation or component disable
            if (this.DataModel == null) {
                if (this.CreateDataOnStart) {
                    this.CreateData();
                } else {
                    this.enabled = false;
                }
            }
        }
        public override void CreateData() {

            if (!this.IsBuilt) {
                this.BuildView();
            }

            T data = (T) Activator.CreateInstance(typeof(T));
            K model = Activator.CreateInstance(typeof(K)) as K;
            model.Bind(data, null);

            this.BindData(model);
        }
        
        public override void NotifyAll() {
            this.OnValueUpdated(this.DataModel.Value);
            foreach (List<ViewModel> viewModels in this.BindingMap.Values) {
                foreach (ViewModel viewModel in viewModels) {
                    viewModel.NotifyAll();
                }
            }
        }

        protected override void OnDestroy() {
            if (this.DataModel != null) {
                this.UnBindData();
            }
            base.OnDestroy();
        }
        public override void BindData(DataModel dataModel) { 
            base.BindData(dataModel);
            K genericModel = dataModel as K;
            if (genericModel != null) {
                this.BindData(genericModel);
            } else {
                Debug.LogError($"Could not cast data model of type {dataModel.GetType().FullName} to {typeof(K).FullName}");
            }
        }

        public virtual void BindData(K dataModel) {
            this.DataModel = dataModel;
            this.DataModel.OnValueUpdated += this.OnValueUpdated;
            foreach (KeyValuePair<string,List<ViewModel>> keyVal in this.BindingMap) {
                string bindingContext = keyVal.Key;
                DataModel data = this.DataModel.Find(bindingContext);
                if (data != null) {
                    List<ViewModel> boundViews = keyVal.Value;
                    foreach (ViewModel view in boundViews) {
                        view.BindData(data);
                    }
                } else {
                    // [ViewBind] can be used to easily capture other type of components too, so we have to allow null when looking for data model
                    // if we want to be more strict then we could bind components with a distict attribute eg [ComponentBind]
                    // Debug.Log($"Could not find data model for binding context '{bindingContext}'");
                }
            }
            foreach (KeyValuePair<string,Delegate> keyVal in this.ActionMap) {
                string bindingContext = keyVal.Key;
                Delegate action = keyVal.Value;
                DataModel data = this.FindDataContext(bindingContext);
                if (data == null) {
                    Debug.LogError($"{this} {bindingContext}");
                }
                data.BindAction(action);
            }
            this.OnBound();
            this.SetupView();
            this.enabled = true;
        }

        public override void UnBindData() {
            if (this.DataModel == null || RuntimeUtils.IsQuitting) return;
            
            this.OnUnBind();
            this.DataModel.OnValueUpdated -= this.OnValueUpdated;
            foreach (List<ViewModel> boundViews in this.BindingMap.Values) {
                foreach (ViewModel view in boundViews) {
                    if (view.GetDataModel() != null) {
                        view.UnBindData();
                    }
                }
            }
            foreach (KeyValuePair<string,Delegate> keyVal in this.ActionMap) {
                string bindingContext = keyVal.Key;
                Delegate action = keyVal.Value;
                DataModel data = this.FindDataContext(bindingContext);
                data.UnBindAction(action);
            }
            this.DataModel = null;
            this.enabled = false;
        }

        protected DataModel FindDataContext(string path) {
            DataModel data = null;
            if (path.Contains(".")) {
                path = path.Replace(".", "/");
            }
            if (path.Contains("/")) {
                string[] parts = path.Split("/");
                data = this.DataModel;
                for (int i = 0; i < parts.Length; i++) {
                    data = data.Find(parts[i]);
                }
            } else {
                data = this.DataModel.Find(path);
            }
            return data;
        }

        protected virtual void SetupView() { 
            
        }

        protected virtual void OnValueUpdated(T value) {

        }

        protected virtual void OnBound() {

        }

        protected virtual void OnUnBind() {
            
        }
    }
    public class ListViewModel<T> : ListViewModel<T,DataModel<T>,ViewModel<T>> {}
    public class ListViewModel<T,U,K> : ViewModel<List<T>> where U : DataModel<T> where K : ViewModel<T,U> {

        [ViewBind]
        public K ElementPrefab;
        public AssetReferenceGameObject ElementAssetReference;
        private bool loadingAsset;
        private bool buildViewOnLoaded;

        protected Dictionary<T,K> DataElementMap { get; private set; } //= new Dictionary<T,K>();
        protected List<K> ActiveList { get; private set; } //= new List<K>();
        protected Queue<K> InactiveQueue { get; private set; } //= new List<K>();

        private bool collectionsInitialized;

        protected override void Awake() {
            base.Awake();
            this.InitializeCollections();
            if (this.ElementPrefab == null && this.ElementAssetReference != null) {
                this.loadingAsset = true;
                AsyncOperationHandle<GameObject> asyncOp = this.ElementAssetReference.InstantiateAsync(this.transform); 
                asyncOp.Completed += (op) => {
                    this.loadingAsset = false;
                    this.ElementPrefab = op.Result.GetComponent<K>();
                    this.ElementPrefab.gameObject.SetActive(false);
                    this.ElementPrefab.gameObject.name = "ElementPrefab";
                    if (this.buildViewOnLoaded) {
                        this.BuildList();
                    }
                };
            }
        }

        private void InitializeCollections() {
            if (this.collectionsInitialized) return;
            this.collectionsInitialized = true;
            this.DataElementMap = GenericPool<Dictionary<T,K>>.Get();
            this.ActiveList = GenericPool<List<K>>.Get();
            this.InactiveQueue = GenericPool<Queue<K>>.Get();
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            this.DataElementMap.Clear();
            GenericPool<Dictionary<T,K>>.Recycle(this.DataElementMap);
            this.ActiveList.Clear();
            GenericPool<List<K>>.Recycle(this.ActiveList);
            this.InactiveQueue.Clear();
            GenericPool<Queue<K>>.Recycle(this.InactiveQueue);

            if (this.ElementAssetReference != null && this.ElementAssetReference.Asset != null) {
                this.ElementAssetReference.ReleaseAsset();
            }
        }

        protected override void SetupView() { 
            this.InitializeCollections();
            if (this.loadingAsset) {
                this.buildViewOnLoaded = true;
            } else {
                this.BuildList();    
            }
        }

        protected virtual void BuildList() {
            this.buildViewOnLoaded = false;
            int count = 0;
            if (this.DataModel.Value != null) {
                count = this.DataModel.Value.Count;
            }
            this.SetElementCount(count);
            for (int i = 0; i < count; i++) {
                U datamodel = this.DataModel.GetModelAt(i) as U;
                this.ActiveList[i].BindData(datamodel);
                this.DataElementMap[datamodel.Value] = this.ActiveList[i];
            }
        }

        public override void BindData(DataModel<List<T>> dataModel) {
            base.BindData(dataModel);
            this.DataModel.OnElementAdded += this.OnElementAdded;
            this.DataModel.OnElementRemoved += this.OnElementRemoved;
            this.DataModel.OnElementUpdated += this.OnElementUpdated;
        }

        public override void UnBindData() {
            if (this.DataModel == null) return;
            this.DataModel.OnElementAdded -= this.OnElementAdded;
            this.DataModel.OnElementRemoved -= this.OnElementRemoved;
            this.DataModel.OnElementUpdated -= this.OnElementUpdated;
            base.UnBindData();
        }

        protected override void OnValueUpdated(List<T> value) {
            this.BuildList();
        }

        protected virtual void OnElementAdded(object data) {
            this.OnElementAdded((T)data);
        }

        protected virtual void OnElementRemoved(object data) {
            this.OnElementRemoved((T)data);
        }

        protected virtual void OnElementUpdated(int index, object data) {
            this.OnElementUpdated(index, (T)data);
        }

        protected virtual void OnElementAdded(T data) {
            if (this.loadingAsset) return;
            K element = this.ActivateElement();
            this.DataElementMap[data] = element;
            element.BindData(this.DataModel.GetModelAt(this.DataModel.ModelList.Count - 1));
        }

        protected virtual void OnElementRemoved(T data) {
            if (this.DataElementMap.TryGetValue(data, out K element)) {
                this.DeactivateElement(element);
            }
        }

        protected virtual void OnElementUpdated(int index, T data) {
            if (this.DataElementMap.TryGetValue(data, out K element)) {
                element.BindData(this.DataModel.GetModelAt(index));
            }
        }

        protected virtual void SetElementCount(int count) {
            while (this.ActiveList.Count < count) {
                this.ActivateElement();
            }
            while (this.ActiveList.Count > count) {
                this.DeactivateElement(this.ActiveList[^1]);
            }
        }

        protected K ActivateElement() {
            K element;
            if (this.InactiveQueue.Count > 0) {
                element = this.InactiveQueue.Dequeue();
            } else {
                element = Instantiate(this.ElementPrefab.gameObject, this.transform).GetComponent<K>();
                if (!element.IsBuilt) {
                    element.BuildView();
                }
            }
            element.gameObject.SetActive(true);
            this.ActiveList.Add(element);
            return element;
        }

        protected void DeactivateElement(K element) {
            element.gameObject.SetActive(false);
            this.ActiveList.Remove(element);
            this.InactiveQueue.Enqueue(element);
            this.DataElementMap.Remove(element.DataModel.Value);
        }
    }
}
