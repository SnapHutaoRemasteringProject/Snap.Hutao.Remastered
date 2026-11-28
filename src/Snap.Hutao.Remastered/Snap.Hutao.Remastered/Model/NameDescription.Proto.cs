// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Metadata.Avatar;

namespace Snap.Hutao.Remastered.Model;

// Kept out of NameDescription.cs because the test project links that file without the metadata
// Avatar model; the include is only needed by the application's bundle model.
[ProtoInclude(100, typeof(AvatarNameCard))]
public partial class NameDescription;
