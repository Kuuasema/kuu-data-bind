using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Kuuasema.DataBinding;



public class Player {
    [DataBind] public string PlayerName;
    [DataBind] public int Level;
    [DataBind] public int XP;
    [DataBind] public int RequiredXP;
    [DataBind] public float LevelProgress;
    [DataBind] public string ProgressText;
}

public class PlayerDataModel : DataModel<Player> {
    [PropertyBind] public DataModel<string> PlayerName { get; private set; }
    [PropertyBind] public DataModel<int> Level { get; private set; }
    [PropertyBind] public DataModel<int> XP { get; private set; }
    [PropertyBind] public DataModel<int> RequiredXP { get; private set; }
    [PropertyBind] public DataModel<float> LevelProgress { get; private set; }
    [PropertyBind] public DataModel<string> ProgressText { get; private set; }

    public void AddXP(int amount) {
        this.XP.AddValue(amount);
        if (this.XP.Value >= this.RequiredXP.Value) {
            this.LevelUp();
        }
        this.LevelProgress.SetValue((float)this.XP.Value / (float)this.RequiredXP.Value);
        this.ProgressText.SetValue($"{this.XP.Value}/{this.RequiredXP.Value}");
    }

    public void LevelUp() {
        int carryXP = this.XP.Value % this.RequiredXP.Value;
        this.Level.AddValue(1);
        this.XP.SetValue(carryXP);
        this.RequiredXP.SetValue(this.RequiredXP.Value * 2);
        
        Demo.DataModel.MissionGold.Reset();
        Demo.DataModel.MissionPlay.Reset();
        Demo.DataModel.MissionKill.Reset();

        Demo.DataModel.Messages.AddValue(new Message() {
            Subject = $"Level Up ({this.Level.Value})",
            Text = $"You have reached level {this.Level.Value}"
        });

        Demo.UpdateMessagesNotifications();
    }
    
}

public class PlayerViewModel : ViewModel<Player,PlayerDataModel> {
    [ViewBind] public ViewModel<string> PlayerName;
    [ViewBind] public ViewModel<int> Level;
    // [ViewBind] public ViewModel<int> XP;
    // [ViewBind] public ViewModel<int> RequiredXP;
    [ViewBind] public ViewModel<float> LevelProgress;
    [ViewBind] public ViewModel<string> ProgressText;
}
