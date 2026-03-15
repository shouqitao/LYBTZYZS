using System.Collections.ObjectModel;
using LYBT.Desktop.Sync.Services;
using LYBT.Desktop.Sync.ViewModels;
using LYBT.Shared.Models.Contracts.Sync;

namespace LYBT.Tests.Desktop.PureLogic.Sync;

public class SyncResolutionBuilderTests
{
    #region Build Tests

    [Fact]
    public void Build_WithSelectedLocalOnlyItems_AddsToToUpload()
    {
        var localOnlyItems = new ObservableCollection<SyncItemViewModel>
        {
            CreateSyncItemViewModel(Guid.NewGuid(), true),
            CreateSyncItemViewModel(Guid.NewGuid(), false)
        };
        var serverOnlyItems = new ObservableCollection<SyncItemViewModel>();
        var conflictItems = new ObservableCollection<SyncItemViewModel>();

        var resolution = SyncResolutionBuilder.Build(localOnlyItems, serverOnlyItems, conflictItems);

        Assert.Single(resolution.ToUpload);
        Assert.Equal(localOnlyItems[0].EntityId, resolution.ToUpload[0]);
    }

    [Fact]
    public void Build_WithSelectedServerOnlyItems_AddsToToDownload()
    {
        var localOnlyItems = new ObservableCollection<SyncItemViewModel>();
        var serverOnlyItems = new ObservableCollection<SyncItemViewModel>
        {
            CreateSyncItemViewModel(Guid.NewGuid(), true),
            CreateSyncItemViewModel(Guid.NewGuid(), false)
        };
        var conflictItems = new ObservableCollection<SyncItemViewModel>();

        var resolution = SyncResolutionBuilder.Build(localOnlyItems, serverOnlyItems, conflictItems);

        Assert.Single(resolution.ToDownload);
        Assert.Equal(serverOnlyItems[0].EntityId, resolution.ToDownload[0]);
    }

    [Fact]
    public void Build_WithResolvedConflicts_AddsToConflictResolutions()
    {
        var localOnlyItems = new ObservableCollection<SyncItemViewModel>();
        var serverOnlyItems = new ObservableCollection<SyncItemViewModel>();
        var conflictItems = new ObservableCollection<SyncItemViewModel>
        {
            CreateConflictItem(Guid.NewGuid(), true, true),
            CreateConflictItem(Guid.NewGuid(), true, false),
            CreateConflictItem(Guid.NewGuid(), false, null)
        };

        var resolution = SyncResolutionBuilder.Build(localOnlyItems, serverOnlyItems, conflictItems);

        Assert.Equal(2, resolution.ConflictResolutions.Count);
        Assert.True(resolution.ConflictResolutions[conflictItems[0].EntityId]);
        Assert.False(resolution.ConflictResolutions[conflictItems[1].EntityId]);
    }

    [Fact]
    public void Build_WithSkippedConflicts_AddsToSkipped()
    {
        var localOnlyItems = new ObservableCollection<SyncItemViewModel>();
        var serverOnlyItems = new ObservableCollection<SyncItemViewModel>();
        var conflictItems = new ObservableCollection<SyncItemViewModel>
        {
            CreateConflictItem(Guid.NewGuid(), false, null),
            CreateConflictItem(Guid.NewGuid(), true, true)
        };

        var resolution = SyncResolutionBuilder.Build(localOnlyItems, serverOnlyItems, conflictItems);

        Assert.Single(resolution.Skipped);
        Assert.Equal(conflictItems[0].EntityId, resolution.Skipped[0]);
    }

    [Fact]
    public void Build_WithUnresolvedSelectedConflicts_DoesNotAddToConflictResolutions()
    {
        var localOnlyItems = new ObservableCollection<SyncItemViewModel>();
        var serverOnlyItems = new ObservableCollection<SyncItemViewModel>();
        var conflictItems = new ObservableCollection<SyncItemViewModel>
        {
            CreateConflictItem(Guid.NewGuid(), true, null)
        };

        var resolution = SyncResolutionBuilder.Build(localOnlyItems, serverOnlyItems, conflictItems);

        Assert.Empty(resolution.ConflictResolutions);
    }

    [Fact]
    public void Build_WithAllTypes_CreatesCompleteResolution()
    {
        var localOnlyItems = new ObservableCollection<SyncItemViewModel>
        {
            CreateSyncItemViewModel(Guid.NewGuid(), true)
        };
        var serverOnlyItems = new ObservableCollection<SyncItemViewModel>
        {
            CreateSyncItemViewModel(Guid.NewGuid(), true)
        };
        var conflictItems = new ObservableCollection<SyncItemViewModel>
        {
            CreateConflictItem(Guid.NewGuid(), true, true),
            CreateConflictItem(Guid.NewGuid(), false, null)
        };

        var resolution = SyncResolutionBuilder.Build(localOnlyItems, serverOnlyItems, conflictItems);

        Assert.Single(resolution.ToUpload);
        Assert.Single(resolution.ToDownload);
        Assert.Single(resolution.ConflictResolutions);
        Assert.Single(resolution.Skipped);
    }

    #endregion

    #region HasDataToSync Tests

    [Fact]
    public void HasDataToSync_WithSelectedLocalOnlyItems_ReturnsTrue()
    {
        var localOnlyItems = new ObservableCollection<SyncItemViewModel>
        {
            CreateSyncItemViewModel(Guid.NewGuid(), true)
        };
        var serverOnlyItems = new ObservableCollection<SyncItemViewModel>();
        var conflictItems = new ObservableCollection<SyncItemViewModel>();

        var result = SyncResolutionBuilder.HasDataToSync(localOnlyItems, serverOnlyItems, conflictItems);

        Assert.True(result);
    }

    [Fact]
    public void HasDataToSync_WithSelectedServerOnlyItems_ReturnsTrue()
    {
        var localOnlyItems = new ObservableCollection<SyncItemViewModel>();
        var serverOnlyItems = new ObservableCollection<SyncItemViewModel>
        {
            CreateSyncItemViewModel(Guid.NewGuid(), true)
        };
        var conflictItems = new ObservableCollection<SyncItemViewModel>();

        var result = SyncResolutionBuilder.HasDataToSync(localOnlyItems, serverOnlyItems, conflictItems);

        Assert.True(result);
    }

    [Fact]
    public void HasDataToSync_WithSelectedConflictItems_ReturnsTrue()
    {
        var localOnlyItems = new ObservableCollection<SyncItemViewModel>();
        var serverOnlyItems = new ObservableCollection<SyncItemViewModel>();
        var conflictItems = new ObservableCollection<SyncItemViewModel>
        {
            CreateConflictItem(Guid.NewGuid(), true, true)
        };

        var result = SyncResolutionBuilder.HasDataToSync(localOnlyItems, serverOnlyItems, conflictItems);

        Assert.True(result);
    }

    [Fact]
    public void HasDataToSync_WithNoSelectedItems_ReturnsFalse()
    {
        var localOnlyItems = new ObservableCollection<SyncItemViewModel>
        {
            CreateSyncItemViewModel(Guid.NewGuid(), false)
        };
        var serverOnlyItems = new ObservableCollection<SyncItemViewModel>
        {
            CreateSyncItemViewModel(Guid.NewGuid(), false)
        };
        var conflictItems = new ObservableCollection<SyncItemViewModel>
        {
            CreateConflictItem(Guid.NewGuid(), false, null)
        };

        var result = SyncResolutionBuilder.HasDataToSync(localOnlyItems, serverOnlyItems, conflictItems);

        Assert.False(result);
    }

    [Fact]
    public void HasDataToSync_WithEmptyCollections_ReturnsFalse()
    {
        var localOnlyItems = new ObservableCollection<SyncItemViewModel>();
        var serverOnlyItems = new ObservableCollection<SyncItemViewModel>();
        var conflictItems = new ObservableCollection<SyncItemViewModel>();

        var result = SyncResolutionBuilder.HasDataToSync(localOnlyItems, serverOnlyItems, conflictItems);

        Assert.False(result);
    }

    #endregion

    #region GetCounts Tests

    [Fact]
    public void GetCounts_ReturnsCorrectCounts()
    {
        var localOnlyItems = new ObservableCollection<SyncItemViewModel>
        {
            CreateSyncItemViewModel(Guid.NewGuid(), true),
            CreateSyncItemViewModel(Guid.NewGuid(), false)
        };
        var serverOnlyItems = new ObservableCollection<SyncItemViewModel>
        {
            CreateSyncItemViewModel(Guid.NewGuid(), true)
        };
        var conflictItems = new ObservableCollection<SyncItemViewModel>
        {
            CreateConflictItem(Guid.NewGuid(), true, true),
            CreateConflictItem(Guid.NewGuid(), false, null)
        };

        var (uploadCount, downloadCount, conflictCount, totalCount) =
            SyncResolutionBuilder.GetCounts(localOnlyItems, serverOnlyItems, conflictItems);

        Assert.Equal(1, uploadCount);
        Assert.Equal(1, downloadCount);
        Assert.Equal(2, conflictCount);
        Assert.Equal(5, totalCount);
    }

    [Fact]
    public void GetCounts_WithEmptyCollections_ReturnsZeros()
    {
        var localOnlyItems = new ObservableCollection<SyncItemViewModel>();
        var serverOnlyItems = new ObservableCollection<SyncItemViewModel>();
        var conflictItems = new ObservableCollection<SyncItemViewModel>();

        var (uploadCount, downloadCount, conflictCount, totalCount) =
            SyncResolutionBuilder.GetCounts(localOnlyItems, serverOnlyItems, conflictItems);

        Assert.Equal(0, uploadCount);
        Assert.Equal(0, downloadCount);
        Assert.Equal(0, conflictCount);
        Assert.Equal(0, totalCount);
    }

    #endregion

    #region Helper Methods

    private static SyncItemViewModel CreateSyncItemViewModel(Guid entityId, bool isSelected)
    {
        return new SyncItemViewModel
        {
            EntityId = entityId,
            EntityType = "TestEntity",
            EntityName = "Test Entity",
            DiffType = SyncDiffType.LocalOnly,
            IsSelected = isSelected
        };
    }

    private static SyncItemViewModel CreateConflictItem(Guid entityId, bool isSelected, bool? resolutionDecision)
    {
        return new SyncItemViewModel
        {
            EntityId = entityId,
            EntityType = "TestEntity",
            EntityName = "Test Entity",
            DiffType = SyncDiffType.Modified,
            IsSelected = isSelected,
            ResolutionDecision = resolutionDecision
        };
    }

    #endregion
}
