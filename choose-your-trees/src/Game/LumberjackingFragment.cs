using System.ComponentModel;
using Bindito.Core;
using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using Timberborn.Forestry;
using Timberborn.Localization;
using Timberborn.PrioritySystem;
using Timberborn.PrioritySystemUI;
using Timberborn.WorkSystemUI;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChooseYourTrees.Game;

// This fragment owns its collapse control, including when TImprove 4 UX is installed.
[Description("DoNotCollapse")]
internal sealed class LumberjackingFragment : IEntityPanelFragment
{
    private readonly VisualElementLoader _loader;
    private readonly PriorityToggleGroupFactory _priorityFactory;
    private readonly WorkplacePrioritySpriteLoader _sprites;
    private readonly TreePriorityService _priorities;
    private readonly ILoc _loc;
    private VisualElement _root = null!;
    private VisualElement _content = null!;
    private Button _collapse = null!;
    private Button _expand = null!;
    private PriorityToggleGroup _group = null!;
    private TreeComponent? _tree;
    private bool _expanded = true;

    public LumberjackingFragment(VisualElementLoader loader, PriorityToggleGroupFactory priorityFactory,
        WorkplacePrioritySpriteLoader sprites, TreePriorityService priorities, ILoc loc)
    {
        _loader = loader;
        _priorityFactory = priorityFactory;
        _sprites = sprites;
        _priorities = priorities;
        _loc = loc;
    }

    public VisualElement InitializeFragment()
    {
        // Reuse the actual game panel template, including its NineSlice background and stylesheets.
        _root = _loader.LoadVisualElement("Game/EntityPanel/WorkplaceFragment");
        _root.Clear();
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.alignItems = Align.Center;
        header.style.marginBottom = 5;
        var heading = new Label(_loc.T("ChooseYourTrees.Lumberjacking"));
        heading.AddToClassList("entity-panel__heading-text");
        heading.style.unityFontStyleAndWeight = FontStyle.Bold;
        heading.style.flexGrow = 1;
        header.Add(heading);
        _collapse = new Button(() => SetExpanded(false));
        _collapse.AddToClassList("button-square");
        _collapse.AddToClassList("button-minus");
        _expand = new Button(() => SetExpanded(true));
        _expand.AddToClassList("button-square");
        _expand.AddToClassList("button-plus");
        header.Add(_collapse);
        header.Add(_expand);
        _root.Add(header);
        _content = new VisualElement();
        _root.Add(_content);
        _group = _priorityFactory.Create(_content, "ChooseYourTrees.Priority", _sprites, "", "");
        SetExpanded(_expanded);
        _root.style.display = DisplayStyle.None;
        return _root;
    }

    public void ShowFragment(BaseComponent entity)
    {
        _tree = entity.GetComponent<TreeComponent>();
        if (_tree != null)
        {
            _group.Enable(new TreePrioritizable(_tree, _priorities));
            _root.style.display = DisplayStyle.Flex;
        }
    }

    public void ClearFragment()
    {
        if (_tree != null) _group.Disable();
        _tree = null;
        _root.style.display = DisplayStyle.None;
    }

    public void UpdateFragment()
    {
        if (_tree != null) _group.UpdateGroup();
    }

    private void SetExpanded(bool expanded)
    {
        _expanded = expanded;
        _content.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
        _collapse.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
        _expand.style.display = expanded ? DisplayStyle.None : DisplayStyle.Flex;
    }

    private sealed class TreePrioritizable : IPrioritizable
    {
        private readonly TreeComponent _tree;
        private readonly TreePriorityService _priorities;
        public TreePrioritizable(TreeComponent tree, TreePriorityService priorities) { _tree = tree; _priorities = priorities; }
        public Priority Priority => (Priority)_priorities.GetPriority(_tree);
        public void SetPriority(Priority priority) => _priorities.SetPriority(_tree, (int)priority, "tree panel");
    }
}

internal sealed class TreePanelProvider : IProvider<EntityPanelModule>
{
    private readonly LumberjackingFragment _fragment;
    public TreePanelProvider(LumberjackingFragment fragment) => _fragment = fragment;
    public EntityPanelModule Get()
    {
        var builder = new EntityPanelModule.Builder();
        builder.AddMiddleFragment(_fragment);
        return builder.Build();
    }
}
