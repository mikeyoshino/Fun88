namespace Fun88.Web.Modules.Games.ViewModels;

using System;
using System.Collections.Generic;

public record GameDetailViewModel(
    Guid Id,
    string Slug,
    string Title,
    string? Description,
    string? ControlDescription,
    string ThumbnailUrl,
    string EmbedUrl,
    long PlayCount,
    long LikeCount,
    IReadOnlyList<string> CategoryNames,
    IReadOnlyList<GameCardViewModel> RelatedGames
);
