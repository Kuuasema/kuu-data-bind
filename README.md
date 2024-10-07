KUU DATA BIND
============

ABOUT
------------
This package contains a data binding system.  
It depends on the Utils package.  

SETUP
------------
Add this package to your project with the Package Manager by giving it this git url. Unity will try to download the package using git. 
Depending on your machine you may have to run additional commands to make sure Unity has the permission to do it:
 > **OSX**
 > ```  
 > ssh-add
 > ```  

FEATURES
============

DDMVM
------------


This data binding framework is tailored for Unity's MonoBehaviours.  
It is based on the premise that anything can be data by adding data bind attributes to fields.
Primitive types dont need attributes but obviously wont have child data fields either.  

There are three key components to this system:  
- Data  
- DataModel  
- ViewModel  

The system utilizes Attributes, Generics and Reflection. Both DataModel and ViewModel are template classes with the Data type as the template argument. Additionally the ViewModel derives from MonoBehaviour so it is a component that should be attached on a game object.


Data
------------
So the data can be any type (primitive, struct or class) and does not have to be marked with any attributes either. Only if the data contains child data then those fields should be marked with the binding attribute: ```[DataBind]```   

**TODO**  

DataModel< Data >
------------

**TODO**  


ViewModel< Data >
------------

**TODO**  

ADDITIONAL FEATURES
============

List View Models (Spawners)
------------
In case of the data being a list of something, then instead of using a view model of that same list type, instead use a list view model of the type:  
```
List<string> messages;
DataModel<List<string>> messagesDataModel;
ListViewModel<string> messagesViewModel;
```  

Defining the class for the ListViewModel requires one additional parameter, the concrete view model type that it should spawn, which determines what prefabs can be assigned to its inspector slot:  
```
public class MessagesViewModel : ListViewModel<string,DataModel<string>,MessageViewModel> {}

public class MessageViewModel : ViewModel<string> {} // the view model for a message

```

So a concrete script example would be:   
```
/// DATA
public class Inbox {
	[DataBind] List<string> Messages;
}

/// DATA MODEL
public class InboxModel : DataModel<Inbox> {
	[PropertyBind] public DataModel<List<string>> Messages { get; private set; }
}

/// VIEW MODEL
public class MessageView : ViewModel<string> {}
public class MessageListView : ListViewModel<string,DataModel<string>,MessageView> {}

public class InboxView : ViewModel<Inbox> {
	[ViewBind] MessageListView Messages;
}


```

Property Inherit Attribute
------------
The property inherit attribute allows a data model to reference parent data. This allows child data and view hierachies to respond (and modify) that parent data. This is especially useful when the data contains lists, where something meaningful for the context cannot (or preferred not) be stored within the instance in question.  

Script Example:  
```
/// DATA
public enum FireMode { Safety, Single, FullAuto }

public class WeaponAction { 
	protected virtual FireMode Mode => FireMode.Safety;
}
public class SafetyAction : WeaponAction {}
public class SingleAction : WeaponAction {
	protected override FireMode Mode => FireMode.Single;
}
public class FullAutoAction : WeaponAction {
	protected override FireMode Mode => FireMode.FullAuto;
}

public class Weapon {
	[DataBind] public FireMode CurrentMode;
	[DataBind] public List<WeaponAction> Actions;
}

/// DATA MODEL

public class WeaponModel : DataModel<Weapon> {
	[PropertyBind] public DataModel<FireMode> CurrentMode { get; private set; }
	[PropertyBind] public DataModel<List<WeaponAction>> Actions { get; private set; }
}

public class WeaponActionModel : DataModel<WeaponAction> {
	[PropertyInherit] public DataModel<FireMode> CurrentMode { get; private set; }
}

/// VIEW MODEL

public class WeaponActionView : ViewModel<WeaponAction,WeaponActionModel> {

	[BindAction("CurrentMode")]
	private void OnSetMode(FireMode mode) {
		this.enable = this.DataModel.Mode.Value == mode;
	}
}


```

Bind Action Attribute
------------
Allows methods with matching signature to be bound to data:   
```
/// DATA
public class Data {
	publis string Message;
}

// VIEW
[BindAction("Message")]
private void OnMessageChanged(string text) {
	this.messageTextField.text = text;
}
```

Bind Button Attribute
------------
Allows boolean data to be bound to a button in a most convenient way. When the button is pressed the underlaying data is set to true and the button stays in pressed mode untill the data is set to false.  
Example:   
```
/// DATA
public class Data {
	publis bool Execute;
}

// VIEW
[BindButton("Execute")]
public Button executeButton;
```

Data Context Map
------------
Sometimes there may not be a convenient way to bind data to view. For those situations data can register into the data context map, from where views can try to find data to bind with.  

Example:  
```
/// DATA
public class Settings {}
public class SettingsModel : DataModel<Settings> {}

private static SettingsModel settings;
void Init() {
	settings = new SettingsModel();
	DataContextMap.Put("Settings", settings);
}

/// VIEW
public class SettingsView : ViewModel<Settings> {

	protected override void Start() {
		this.BindData(DataContextMap.Find<SettingsModel>("Settings"));
	}
}
```



