using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Kuuasema.DataBinding;

public enum MissionState {
    InProgress,
    Claimable,
    Claimed
}

public class Mission {
    
    [DataBind] public string Description;
    [DataBind] public int Required;
    [DataBind] public int Collected;
    [DataBind] public float Progress;
    [DataBind] public MissionState State;
}

public class MissionDataModel : DataModel<Mission> {
    [PropertyBind] public DataModel<string> Description { get; private set; }
    [PropertyBind] public DataModel<int> Required { get; private set; }
    [PropertyBind] public DataModel<int> Collected { get; private set; }
    [PropertyBind] public DataModel<float> Progress { get; private set; }
    [PropertyBind] public DataModel<MissionState> State { get; private set; }

    public void Collect(int amount) {
        this.Collected.AddValue(amount);
        this.Progress.SetValue((float) this.Collected.Value / (float) this.Required.Value);
        if (this.Progress.Value >= 1 && this.State.Value == MissionState.InProgress) {
            this.State.SetValue(MissionState.Claimable);
        }
    }

    public void Claim() {
        if (this.State.Value == MissionState.Claimable) {
            this.State.SetValue(MissionState.Claimed);

            this.ClaimReward();
            // Demo.UpdateMissionNotifications();

            Demo.AddMessage(new Message() {
                Subject = $"Mission ({this.Description.Value}) completed.",
                Text = $"You have completed the mission: {this.Description.Value}"
            });
        }
    }

    public void Reset() {
        if (this.State.Value != MissionState.Claimed) {
            return;
        }
        this.Collected.SetValue(0);
        this.Progress.SetValue(0.0f);
        this.State.SetValue(MissionState.InProgress);
    }

    private void ClaimReward() {
        // TODO
    }
}

public class MissionViewModel : ViewModel<Mission,MissionDataModel> {

    [ViewBind] public ViewModel<string> Description;
    // [ViewBind] public ViewModel<int> Required;
    // [ViewBind] public ViewModel<int> Collected;
    [ViewBind] public ViewModel<float> Progress;
    [ViewBind] public List<ViewModel<MissionState>> State;
}
