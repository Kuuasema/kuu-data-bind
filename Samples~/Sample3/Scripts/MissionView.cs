using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Kuuasema.DataBinding;


public class MissionView : MissionViewModel {
    public void Claim() {
        this.DataModel.Claim();
    }
}


