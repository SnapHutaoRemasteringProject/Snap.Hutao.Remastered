// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Web.Hutao.GachaLog;

namespace Snap.Hutao.Remastered.ViewModel.GachaLog;

/// <summary>
/// 胡桃云记录操作视图模型
/// </summary>
public sealed partial class HutaoCloudEntryOperationViewModel : ObservableObject
{
    /// <summary>
    /// 构造一个新的胡桃云记录操作视图模型
    /// </summary>
    /// <param name="entry">记录</param>
    /// <param name="retrieve">获取记录</param>
    /// <param name="delete">删除记录</param>
    public HutaoCloudEntryOperationViewModel(GachaEntry entry, ICommand? retrieve, ICommand delete)
    {
        Uid = entry.Uid;
        ItemCount = entry.ItemCount;
        RetrieveCommand = retrieve;
        DeleteCommand = delete;
    }

    public string Uid { get; private set => SetProperty(ref field, value); }

    public int ItemCount { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// 获取云端数据
    /// </summary>
    public ICommand? RetrieveCommand { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// 删除云端数据
    /// </summary>
    public ICommand DeleteCommand { get; private set => SetProperty(ref field, value); }
}
