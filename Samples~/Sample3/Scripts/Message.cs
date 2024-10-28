using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Kuuasema.DataBinding;

public enum MessageState {
    Unread,
    Read
}

public class Message {
    [DataBind] public string Subject;
    [DataBind] public string Text;
    [DataBind] public MessageState State;
}

public class MessageDataModel : DataModel<Message> {
    [PropertyBind] public DataModel<string> Subject { get; private set; }
    [PropertyBind] public DataModel<string> Text { get; private set; }
    [PropertyBind] public DataModel<MessageState> State { get; private set; }

    public void Read() {
        if (this.State.Value == MessageState.Unread) {
            this.State.SetValue(MessageState.Read);
            Demo.UpdateMessagesNotifications();
        }
    }

    public void Remove() {

    }
}

public class MessageViewModel : ViewModel<Message,MessageDataModel> {

    [ViewBind] public ViewModel<string> Subject;
    [ViewBind] public ViewModel<string> Text;
    [ViewBind] public List<ViewModel<MessageState>> State;
}

