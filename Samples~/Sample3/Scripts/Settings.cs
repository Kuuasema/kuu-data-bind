using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Kuuasema.DataBinding;



public class Settings {
    
    [DataBind] public float MusicVolume;
    [DataBind] public float EffectVolume;
    [DataBind] public bool EnableNotice;
    [DataBind] public bool EnablePowerSave;
}

public class SettingsDataModel : DataModel<Settings> {
    
    [PropertyBind] public DataModel<float> MusicVolume { get; private set; }
    [PropertyBind] public DataModel<float> EffectVolume { get; private set; }
    [PropertyBind] public DataModel<bool> EnableNotice { get; private set; }
    [PropertyBind] public DataModel<bool> EnablePowerSave { get; private set; }
}

public class SettingsViewModel : ViewModel<Settings,SettingsDataModel> {
    [ViewBind] public ViewModel<float> MusicVolume;
    [ViewBind] public ViewModel<float> EffectVolume;
    [ViewBind] public List<ViewModel<bool>> EnableNotice;
    [ViewBind] public ViewModel<bool> EnablePowerSave;
}
