using System.Collections.Generic;

namespace GenericGachaRPG
{
    public interface IFormationService
    {
        TeamFormationState CurrentFormation { get; }
        bool HasValidFormation { get; }
        bool IsValidFormation(IReadOnlyList<string> characterIds, out string reason);
        bool TrySetFormation(IReadOnlyList<string> characterIds, out string reason);
    }
}
