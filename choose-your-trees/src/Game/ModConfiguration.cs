using Bindito.Core;
using ModSettings.Core;
using Timberborn.Modding;
using Timberborn.SettingsSystem;

namespace ChooseYourTrees.Game;

internal sealed class TreeModSettings : ModSettingsOwner
{
    public ModSetting<bool> ApplyToTiles { get; } = new(true,
        ModSettingDescriptor.Create("Keep priority on the tile")
            .SetTooltip("On: future trees on a tile inherit its priority. Off: priority belongs to the current tree."));

    public TreeModSettings(ISettings settings, ModSettingsOwnerRegistry registry, ModRepository repository)
        : base(settings, registry, repository) { }

    protected override string ModId => "ChooseYourTrees";
    public override ModSettingsContext ChangeableOn => ModSettingsContext.MainMenu | ModSettingsContext.Game;
}

[Context("MainMenu")]
[Context("Game")]
internal sealed class SettingsConfigurator : Configurator
{
    protected override void Configure() => Bind<TreeModSettings>().AsSingleton();
}

[Context("Game")]
internal sealed class GameConfigurator : Configurator
{
    protected override void Configure()
    {
        Bind<PriorityPersistence>().AsSingleton();
        Bind<TreePriorityService>().AsSingleton();
    }
}
