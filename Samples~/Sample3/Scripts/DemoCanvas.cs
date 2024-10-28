using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Kuuasema.DataBinding;

public class DemoCanvas : DemoViewModel {
    
    protected override void Start() {
        this.BindData(Demo.DataModel);
    }
}
