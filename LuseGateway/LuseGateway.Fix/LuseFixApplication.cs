using System;
using QuickFix;
using QuickFix.Fields;
using Microsoft.Extensions.Logging;
using LuseGateway.Core.Services;

namespace LuseGateway.Fix
{
    public class LuseFixApplication : QuickFix.IApplication
    {
        private readonly ILogger<LuseFixApplication> _logger;
        private readonly LuseMessageCracker _cracker;
        private readonly Interfaces.IFixMessageLogService _logService;
        private SessionID? _activeSessionId;

        public LuseFixApplication(ILogger<LuseFixApplication> logger, LuseMessageCracker cracker, Interfaces.IFixMessageLogService logService)
        {
            _logger = logger;
            _cracker = cracker;
            _logService = logService;
        }

        public void FromAdmin(QuickFix.Message message, SessionID sessionId)
        {
            _logger.LogDebug("FromAdmin: {Message}", message);
            _logService.LogMessageAsync("IN", message.ToString());
        }

        public void FromApp(QuickFix.Message message, SessionID sessionId)
        {
            _logger.LogInformation("FromApp: {Message}", message);
            _logService.LogMessageAsync("IN", message.ToString());
            try
            {
                _cracker.Crack(message, sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cracking message: {Message}", message);
            }
        }

        public void OnCreate(SessionID sessionId)
        {
            _logger.LogInformation("Session created: {SessionID}", sessionId);
        }

        public void OnLogon(SessionID sessionId)
        {
            _activeSessionId = sessionId;
            _logger.LogInformation("Logon successful: {SessionID}", sessionId);
        }

        public void OnLogout(SessionID sessionId)
        {
            _activeSessionId = null;
            _logger.LogInformation("Logout: {SessionID}", sessionId);
        }

        public void ToAdmin(QuickFix.Message message, SessionID sessionId)
        {
            if (message.Header.GetString(Tags.MsgType) == MsgType.LOGON)
            {
                // In production, these would be from configuration/environment
                // For now, mirroring the old app's credentials if available in config
                // message.SetField(new Password("..."));
                message.SetField(new DefaultApplVerID("8")); // FIX50SP2
            }
            _logger.LogDebug("ToAdmin: {Message}", message);
            _logService.LogMessageAsync("OUT", message.ToString());
        }

        public void ToApp(QuickFix.Message message, SessionID sessionId)
        {
            _logger.LogInformation("ToApp: {Message}", message);
            _logService.LogMessageAsync("OUT", message.ToString());
        }

        public SessionID? ActiveSessionId => _activeSessionId;
    }
}
