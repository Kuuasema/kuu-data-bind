using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Kuuasema.DataBinding;


public class MessageListViewModel : ListViewModel<Message,MessageDataModel,MessageView> {

    public void Remove(MessageView messageView) {
        this.DataModel.RemoveValue(messageView.DataModel.Value);
    }

}

