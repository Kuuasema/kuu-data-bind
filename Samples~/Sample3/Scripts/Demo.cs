using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Kuuasema.Utils;
using Kuuasema.DataBinding;


public class Demo {

    [DataBind] public int Gold = 1100000;
    [DataBind] public float Stamina = 1;

    [DataBind] public Season Season;

    [DataBind] public Player Player = new Player() {
        PlayerName = "Player 1",
        Level = 1,
        XP = 0,
        RequiredXP = 1000,
        ProgressText = "0/1000"
    };

    [DataBind] public Category CategorySettings = new Category() { Id = CategoryId.Settings, NotifyCount = 0 };
    [DataBind] public Category CategoryMission = new Category() { Id = CategoryId.Mission, NotifyCount = 0 };
    [DataBind] public Category CategoryMessages = new Category() { Id = CategoryId.Messages, NotifyCount = 0 };
    [DataBind] public CategoryId ActiveCategory = CategoryId.Undefined;

    [DataBind] public Settings Settings = new Settings() {
        MusicVolume = 0.1f,
        EnableNotice = true
    };
    [DataBind] public Mission MissionGold = new Mission() {
        Description = "Collect 1000 gold",
        Required = 1000,
        Collected = 0
    };

    [DataBind] public Mission MissionPlay = new Mission() {
        Description = "Play 10 times",
        Required = 10,
        Collected = 0
    };

    [DataBind] public Mission MissionKill = new Mission() {
        Description = "Kill 100 enemies",
        Required = 100,
        Collected = 0
    };

    [DataBind] public List<Message> Messages = new List<Message>() {
        new Message() {
            Subject = "Welcome",
            Text = "Welcome to the Data Binding Demo."
        }
    };

    /// STATIC INIT PART ///
    private static Demo data;
    public static DemoDataModel DataModel { get; private set; }

    [InitializeStatic]
    private static void InitDemo() {
        data = new Demo();
        DataModel = new DemoDataModel(data);

        DataModel.Settings.MusicVolume.OnValueUpdated += (vol) => { 
            if (vol > 0.8f) {
                DataModel.CategorySettings.NotifyCount.SetValue(1);
            } else {
                DataModel.CategorySettings.NotifyCount.SetValue(0);
            }
        };

        ScheduledUpdater.RequestContinuousFixedUpdate(UpdateDemo);

        DataModel.MissionGold.State.OnValueUpdated += OnMissionStateChange;
        DataModel.MissionPlay.State.OnValueUpdated += OnMissionStateChange;
        DataModel.MissionKill.State.OnValueUpdated += OnMissionStateChange;

        UpdateMessagesNotifications();
    }

    /// STATIC LOGIC PART ///

    public static void SetCategory(CategoryId category) {
        DataModel.ActiveCategory.SetValue(category);
    }

    public static void AddGold(int amount) {
        DataModel.Gold.AddValue(amount);
        DataModel.MissionGold.Collect(amount);
    }

    public static void AddXP(int amount) {
        DataModel.Player.AddXP(amount);
    }

    public static void Play() {
        int xp = Random.Range(20, 40);
        int kill = Random.Range(5, 10);
        int gold = Random.Range(10, 20);

        if (DataModel.Settings.EnableNotice.Value) {
            AddMessage(new Message() {
                Subject = "Play Results",
                Text = $"{xp} XP, {gold} gold, {kill} monsters killed."
            });
        }

        DataModel.MissionPlay.Collect(1);
        DataModel.MissionKill.Collect(kill);
        DataModel.Player.AddXP(xp);
        AddGold(gold);

        if (Random.value < 0.001f) {
            ToggleSeason();
        }
    }

    public static void ToggleSeason() {
        DataModel.Season.SetValue((Season) ((((int)DataModel.Season.Value) + 1) % 2));
    }

    public static void SetSeason(Season season) {
        DataModel.Season.SetValue(season);
    }

    public static void Kill() {
        DataModel.MissionKill.Collect(1);
    }

    public static void AddMessage(Message message) {
        DataModel.Messages.AddValue(message);
        UpdateMessagesNotifications();
    }

    private static void OnMissionStateChange(MissionState state) {
        int notifyCount = 0;
        if (DataModel.MissionGold.State.Value == MissionState.Claimable) {
            notifyCount++;
        }
        if (DataModel.MissionPlay.State.Value == MissionState.Claimable) {
            notifyCount++;
        }
        if (DataModel.MissionKill.State.Value == MissionState.Claimable) {
            notifyCount++;
        }
        DataModel.CategoryMission.NotifyCount.SetValue(notifyCount);
    }

    public static void UpdateMessagesNotifications() {
        int notifyCount = 0;
        foreach (MessageDataModel messageModel in DataModel.Messages.GetEnumerable<MessageDataModel>()) {
            if (messageModel.State.Value == MessageState.Unread) {
                notifyCount++;
            }
        }
        DataModel.CategoryMessages.NotifyCount.SetValue(notifyCount);
    }

    private static void UpdateDemo() {
        if (DataModel.Stamina.Value < 1.0f) {
            float missing = 1.0f - DataModel.Stamina.Value;
            DataModel.Stamina.AddValue(Mathf.Min(Time.fixedDeltaTime, missing));
        }
    }
}

public class DemoDataModel : DataModel<Demo> {
    public DemoDataModel(Demo data) : base(data) {}
    [PropertyBind] public PlayerDataModel Player { get; private set; }
    [PropertyBind] public DataModel<int> Gold { get; private set; }
    [PropertyBind] public DataModel<float> Stamina { get; private set; }
    [PropertyBind] public DataModel<Season> Season { get; private set; }
    [PropertyBind] public CategoryDataModel CategorySettings { get; private set; }
    [PropertyBind] public CategoryDataModel CategoryMission { get; private set; }
    [PropertyBind] public CategoryDataModel CategoryMessages { get; private set; }
    [PropertyBind] public DataModel<CategoryId> ActiveCategory { get; private set; }
    [PropertyBind] public SettingsDataModel Settings { get; private set; }
    [PropertyBind] public MissionDataModel MissionGold { get; private set; }
    [PropertyBind] public MissionDataModel MissionPlay { get; private set; }
    [PropertyBind] public MissionDataModel MissionKill { get; private set; }

    [PropertyBind] public DataModel<List<Message>> Messages { get; private set; }

}

public class DemoViewModel : ViewModel<Demo,DemoDataModel> {
    [ViewBind] public List<PlayerViewModel> Player;
    [ViewBind] public List<ViewModel<int>> Gold;
    [ViewBind] public List<ViewModel<float>> Stamina;
    [ViewBind] public List<SeasonViewModel> Season;
    [ViewBind] public List<CategoryViewModel> CategorySettings;
    [ViewBind] public List<CategoryViewModel> CategoryMission;
    [ViewBind] public List<CategoryViewModel> CategoryMessages;
    [ViewBind] public List<ViewModel<CategoryId>> ActiveCategory;
    [ViewBind] public List<SettingsViewModel> Settings;
    [ViewBind] public List<MissionViewModel> MissionGold;
    [ViewBind] public List<MissionViewModel> MissionPlay;
    [ViewBind] public List<MissionViewModel> MissionKill;
    [ViewBind] public List<MessageListViewModel> Messages;
}
