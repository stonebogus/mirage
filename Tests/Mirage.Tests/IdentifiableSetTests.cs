using Mirage.Common.Collections;
using Mirage.Common.Interfaces;
using Mirage.Common.Lifecycle;

namespace Mirage.Tests;

public class IdentifiableSetTests
{
    private sealed class Item<T>(T identifier) : Destroyable, IIdentifiable<T>
    {
        public T Identifier { get; } = identifier;
    }

    [Fact]
    public void SupportsGuidIdentifiersAndReadOnlyLookup()
    {
        var item = new Item<Guid>(Guid.NewGuid());
        var set = new IdentifiableSet<Guid, Item<Guid>>([item]);
        IReadOnlyIdentifiableSet<Guid, Item<Guid>> view = set;
        Assert.Same(item, view[item.Identifier]);
        Assert.True(view.TryGetValue(item.Identifier, out var found));
        Assert.Same(item, found);
        Assert.True(view.Contains(item.Identifier));
        Assert.True(view.Contains(item));
        Assert.False(view.Contains(new Item<Guid>(item.Identifier)));
        Assert.False(view.TryGetValue(Guid.Empty, out _));
        Assert.Throws<KeyNotFoundException>(() => view[Guid.Empty]);
        Assert.Equal([item], view.ToArray());
        set.Destroy();
        Assert.False(item.Destroyed);
        Assert.Throws<DestroyedObjectException>(() => set.Add(item));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DuplicateBatchDoesNotMutateOrFireEvents(bool conflictsWithExisting)
    {
        var existing = new Item<string>("existing");
        var set = new IdentifiableSet<string, Item<string>>([existing], limit: 2);
        var events = 0;
        set.OnAdd.Connect(_ => events++);
        set.OnRemove.Connect(_ => events++);
        Assert.Throws<InvalidOperationException>(() =>
            set.Add(new("new"), new(conflictsWithExisting ? "existing" : "new"))
        );
        Assert.Equal([existing], set.ToArray());
        Assert.Equal(0, events);
    }

    [Fact]
    public void ConstructorRejectsDuplicateIdentifiers()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new IdentifiableSet<string, Item<string>>([new("same"), new("same")])
        );
    }

    [Fact]
    public void AdditionRemovalAndClearPublishItemsAtTheExpectedTime()
    {
        var set = new IdentifiableSet<string, Item<string>>();
        var first = new Item<string>("first");
        var second = new Item<string>("second");
        List<string> events = [];
        set.OnAdd.Connect(item =>
        {
            Assert.Same(item, set[item.Identifier]);
            events.Add("add:" + item.Identifier);
        });
        set.OnRemove.Connect(item =>
        {
            Assert.False(set.Contains(item.Identifier));
            events.Add("remove:" + item.Identifier);
        });
        set.OnClear.Connect(_ => events.Add("clear:" + set.Count));
        set.Add(first, second);
        Assert.False(set.Remove("missing"));
        Assert.Throws<InvalidOperationException>(() => set.Remove(new Item<string>("first")));
        set.Remove(first);
        set.Add(first);
        // Preserve the underlying dictionary's enumeration order, including reused slots.
        var removalOrder = set.Select(item => "remove:" + item.Identifier).ToArray();
        set.Clear();
        Assert.Equal(
            new[] { "add:first", "add:second", "remove:first", "add:first", "clear:2" }.Concat(
                removalOrder
            ),
            events
        );
        Assert.Empty(set);
        Assert.False(first.Destroyed);
        Assert.False(second.Destroyed);
    }

    [Fact]
    public void RemovalCallbackCanReregisterTheIdentifier()
    {
        var first = new Item<string>("id");
        var replacement = new Item<string>("id");
        var set = new IdentifiableSet<string, Item<string>>([first]);
        set.OnRemove.Connect(_ => set.Add(replacement));
        set.Remove(first);
        Assert.Same(replacement, set["id"]);
    }

    [Fact]
    public void ReentrantAdditionCannotOverwriteAnItem()
    {
        var set = new IdentifiableSet<string, Item<string>>();
        var insertedByCallback = new Item<string>("second");
        set.OnAdd.Connect(item =>
        {
            if (item.Identifier == "first")
                set.Add(insertedByCallback);
        });
        Assert.Throws<InvalidOperationException>(() => set.Add(new("first"), new("second")));
        Assert.Same(insertedByCallback, set["second"]);
    }

    [Fact]
    public void CapacityEvictsBeforePublishingTheNewItem()
    {
        var first = new Item<string>("first");
        var second = new Item<string>("second");
        var set = new IdentifiableSet<string, Item<string>>([first], limit: 1);
        List<string> events = [];
        set.OnRemove.Connect(item => events.Add("remove:" + item.Identifier));
        set.OnAdd.Connect(item => events.Add("add:" + item.Identifier));
        set.Add(second);
        Assert.Equal(["remove:first", "add:second"], events);
        Assert.Same(second, Assert.Single(set));
    }
}
