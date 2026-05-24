namespace A2ADemo.ApiBackend;

public sealed record A2AAgentCard(
    string Name,
    string Description,
    string Version,
    IReadOnlyList<A2ASupportedInterface> SupportedInterfaces,
    IReadOnlyList<A2AAgentSkill> Skills);

public sealed record A2ASupportedInterface(
    string Url,
    string ProtocolBinding,
    string ProtocolVersion);

public sealed record A2AAgentSkill(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<string>? Tags,
    IReadOnlyList<string>? Examples,
    IReadOnlyList<string>? InputModes,
    IReadOnlyList<string>? OutputModes);