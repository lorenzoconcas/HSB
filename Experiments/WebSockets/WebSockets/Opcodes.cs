namespace HSB.Components.WebSockets;

/// <summary>
/// The opcodes for the WebSocket protocol.
/// </summary>
/// <remarks> opcodes 3-7 are reserved for further non-control frames</remarks>
public enum Opcode {
    CONTINUATION = 0,
    TEXT = 1,
    BINARY = 2,
    CLOSE = 8,
    PING = 9,
    PONG = 10
}