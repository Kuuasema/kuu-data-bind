using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Kuuasema.DataBinding;


public class MessageView : MessageViewModel {
    public void Read() {
        this.DataModel.Read();
    }
}
