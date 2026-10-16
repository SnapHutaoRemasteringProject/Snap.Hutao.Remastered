using Snap.Hutao.Remastered.UI.Xaml.Control.Theme;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class PageResourceScopeTest
{
    [TestMethod]
    public void CreationIsDeferredUntilTheFinalPageResourcesAreAvailable()
    {
        int created = 0;
        PageResourceScope<Dictionary> scope = new(() => { created++; return new(); }, Attach);
        Dictionary constructorResources = new();
        Dictionary xamlResources = new();

        Assert.AreEqual(0, created);
        scope.EnsureAttached(xamlResources);

        Assert.AreEqual(0, constructorResources.Children.Count);
        Assert.AreEqual(1, xamlResources.Children.Count);
        Assert.AreEqual(1, created);
    }

    [TestMethod]
    public void NavigationThenLoadingAttachesOnlyOnce()
    {
        PageResourceScope<Dictionary> scope = new(static () => new(), Attach);
        Dictionary owner = new();
        scope.EnsureAttached(owner);
        scope.EnsureAttached(owner);
        Assert.AreEqual(1, owner.Children.Count);
    }

    [TestMethod]
    public void ReplacedResourcesGetANewDictionaryNotAReparentedInstance()
    {
        PageResourceScope<Dictionary> scope = new(static () => new(), Attach);
        Dictionary first = new();
        Dictionary replacement = new();

        scope.EnsureAttached(first);
        scope.EnsureAttached(replacement);

        Assert.AreNotSame(first.Children[0], replacement.Children[0]);
        Assert.AreSame(first, first.Children[0].Owner);
        Assert.AreSame(replacement, replacement.Children[0].Owner);
    }

    [TestMethod]
    public void DifferentPagesDoNotShareTheirDictionaryInstances()
    {
        PageResourceScope<Dictionary> firstScope = new(static () => new(), Attach);
        PageResourceScope<Dictionary> secondScope = new(static () => new(), Attach);
        Dictionary first = new();
        Dictionary second = new();
        firstScope.EnsureAttached(first);
        secondScope.EnsureAttached(second);
        Assert.AreNotSame(first.Children[0], second.Children[0]);
    }

    [TestMethod]
    public void AFailedAttachmentCanRetryWithAFreshDictionary()
    {
        int attempts = 0;
        Dictionary? failed = null;
        PageResourceScope<Dictionary> scope = new(static () => new(), (owner, resource) =>
        {
            if (attempts++ == 0)
            {
                failed = resource;
                throw new InvalidOperationException("Initialization failed");
            }

            Attach(owner, resource);
        });
        Dictionary owner = new();
        Assert.ThrowsExactly<InvalidOperationException>(() => scope.EnsureAttached(owner));
        scope.EnsureAttached(owner);
        Assert.AreEqual(1, owner.Children.Count);
        Assert.AreNotSame(failed, owner.Children[0]);
    }

    [TestMethod]
    public void ReentrantInitializationDoesNotAttachTwice()
    {
        PageResourceScope<Dictionary>? scope = null;
        scope = new(static () => new(), (owner, resource) =>
        {
            scope!.EnsureAttached(owner);
            Attach(owner, resource);
        });
        Dictionary owner = new();
        scope.EnsureAttached(owner);
        Assert.AreEqual(1, owner.Children.Count);
    }

    private static void Attach(Dictionary owner, Dictionary resource)
    {
        // Enforce the same single-parent rule as WinUI instead of using a List
        // that would silently accept a dictionary owned by another page.
        if (resource.Owner is not null) throw new InvalidOperationException("Element is already the child of another element.");
        resource.Owner = owner;
        owner.Children.Add(resource);
    }

    private sealed class Dictionary
    {
        public Dictionary? Owner { get; set; }

        public List<Dictionary> Children { get; } = [];
    }
}
