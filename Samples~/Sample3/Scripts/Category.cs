using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Kuuasema.DataBinding;

public enum CategoryId { 
    Undefined = -1,
    Settings,
    Mission,
    Messages
}

public class Category {
    
    [DataBind] public CategoryId Id;
    [DataBind] public int NotifyCount;
}

public class CategoryDataModel : DataModel<Category> {
    [PropertyBind] public DataModel<CategoryId> Id { get; private set; }
    [PropertyBind] public DataModel<int> NotifyCount { get; private set; }
}

public class CategoryViewModel : ViewModel<Category,CategoryDataModel> {
    [ViewBind]
    public ViewModel<int> NotifyCount;
}
