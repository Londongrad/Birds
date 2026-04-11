namespace Birds.Application.Common.Models;

public sealed record UpsertBirdsResult(int Added, int Updated, int Removed = 0);