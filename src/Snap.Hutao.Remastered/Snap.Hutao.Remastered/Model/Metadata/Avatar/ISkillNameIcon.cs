// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

/// <summary>
/// Shared contract for the <c>SkillPivot</c> header: both <see cref="Skill"/> (talents) and
/// <see cref="ProudSkill"/> (normal/skill/burst) are shown through one template. Binding the
/// template to this interface avoids casting one of the two unrelated model types to the other.
/// </summary>
public interface ISkillNameIcon
{
    string Name { get; }

    string Icon { get; }
}
