// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Endpoint.Hutao;

public interface IInfrastructureGitRepositoryEndpoints : IInfrastructureRootAccess
{
    /// <summary>
    /// 获取指定名称的 Git 仓库信息。
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    string GitRepository(string name)
    {
        return $"{Root}/git-repository/all?name={name}";
    }

    /// <summary>
    /// 获取所有 Git 仓库信息。
    /// </summary>
    /// <returns></returns>
    string GitRepositoryAll()
    {
        return $"{Root}/git-repository/all";
    }
}