using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

using Kuuasema.Utils;
using Kuuasema.DataBinding;

public class OnOffToggle : ToggleViewModel {

    [ViewBind("On")] private RectTransform on;
    [ViewBind("Off")] private RectTransform off;

    protected override void SetupView() { 
        base.SetupView();
        if (this.DataModel == null) {
            return;
        }
        this.OnValueUpdated(this.DataModel.Value);
    }

    protected override void OnValueUpdated(bool value) {
        base.OnValueUpdated(value);
        this.on.gameObject.TrySetActive(value);
        this.off.gameObject.TrySetActive(!value);
    }
}
