using Bindito.Core;
using Timberborn.BaseComponentSystem;
using Timberborn.WorkSystemUI;
using Timberborn.EntityPanelSystem;
using Timberborn.Forestry;
using Timberborn.Localization;
using Timberborn.PrioritySystem;
using Timberborn.PrioritySystemUI;
using UnityEngine.UIElements;

namespace ChooseYourTrees.Game;

internal sealed class LumberjackingFragment : IEntityPanelFragment
{
    private readonly PriorityToggleGroupFactory _priorityFactory;
    private readonly WorkplacePrioritySpriteLoader _sprites;
    private readonly TreePriorityService _priorities;
    private readonly ILoc _loc;
    private VisualElement _root = null!;
    private PriorityToggleGroup _group = null!;
    private TreeComponent? _tree;

    public LumberjackingFragment(PriorityToggleGroupFactory priorityFactory, WorkplacePrioritySpriteLoader sprites, TreePriorityService priorities, ILoc loc)
    {
        _priorityFactory = priorityFactory;
        _sprites = sprites;
        _priorities = priorities;
        _loc = loc;
    }

    public VisualElement InitializeFragment()
    {
        _root = new VisualElement();
        _root.AddToClassList("entity-panel-fragment");
        var heading = new Label(_loc.T("ChooseYourTrees.Lumberjacking"));
        heading.AddToClassList("entity-panel-fragment__title");
        _root.Add(heading);
        _group = _priorityFactory.Create(_root, "ChooseYourTrees.Priority", _sprites, "", "");
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

    private sealed class TreePrioritizable : IPrioritizable
    {
        private readonly TreeComponent _tree;
        private readonly TreePriorityService _priorities;
        public TreePrioritizable(TreeComponent tree, TreePriorityService priorities) { _tree = tree; _priorities = priorities; }
        public Priority Priority => (Priority)_priorities.GetPriority(_tree);
        public void SetPriority(Priority priority) => _priorities.SetPriority(_tree, (int)priority);
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
