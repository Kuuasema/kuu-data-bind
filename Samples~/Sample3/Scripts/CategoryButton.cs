using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Kuuasema.DataBinding;


public class CategoryButton : CategoryViewModel {
    [ViewBind]
    private Button button;

    protected override void Awake() {
        base.Awake();
        this.button.onClick.AddListener(this.OnClick);
    }

    private void OnClick() {
        if (this.DataModel == null) {
            return;
        }
        Demo.SetCategory(this.DataModel.Id.Value);
    }
}


