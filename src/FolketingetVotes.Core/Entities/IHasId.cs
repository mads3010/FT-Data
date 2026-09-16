namespace FolketingetVotes.Core.Entities;

/// <summary>An entity keyed by the integer id assigned by oda.ft.dk.</summary>
public interface IHasId
{
    int Id { get; }
}
