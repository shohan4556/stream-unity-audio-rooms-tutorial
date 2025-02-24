using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Core;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Libs.Auth;
using UnityEngine;
using UnityEngine.Android;

public class AudioRoomsManager : MonoBehaviour
{
    [SerializeField]
    private string _apiKey;
    
    [SerializeField]
    private string _userId;
    
    [SerializeField]
    private string _userToken;
    
    public IStreamVideoClient StreamClient { get; private set; }
    private IStreamCall _activeCall;
    
    public event Action<IStreamVideoCallParticipant> ParticipantJoined;
    public event Action<string> ParticipantLeft;
    
    protected async void Awake()
    {
        // Request microphone permissions
        Permission.RequestUserPermission(Permission.Microphone);

        // Check if user granted microphone permission
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            // todo Notify user that microphone permission was not granted and the microphone capturing will not work.
        }
        
        // Create Client instance
        StreamClient = StreamVideoClient.CreateDefaultClient();
        
        var credentials = new AuthCredentials(_apiKey, _userId, _userToken);

        try
        {
            // Connect user to Stream server
            await StreamClient.ConnectUserAsync(credentials);
            Debug.Log($"User `{_userId}` is connected to Stream server");
        }
        catch (Exception e)
        {
            // Log potential issues that occured during trying to connect
            Debug.LogException(e);
        }
    }
    
    public async Task JoinCallAsync(string callId)
    {
        _activeCall = await StreamClient.JoinCallAsync(StreamCallType.Default, callId, create: true, ring: false, notify: true);

        // Handle already present participants
        foreach (var participant in _activeCall.Participants)
        {
            OnParticipantJoined(participant);
        }

        // Subscribe to events in order to react to participant joining or leaving the call
        _activeCall.ParticipantJoined += OnParticipantJoined;
        _activeCall.ParticipantLeft += OnParticipantLeft;
    }

    public async Task LeaveCallAsync()
    {
        if (_activeCall == null)
        {
            Debug.LogWarning("Leave request ignored. There is no active call to leave.");
            return;
        }
    
        // Unsubscribe from events 
        _activeCall.ParticipantJoined -= OnParticipantJoined;
        _activeCall.ParticipantLeft -= OnParticipantLeft;
    
        await _activeCall.LeaveAsync();
    }
    
    private void OnParticipantJoined(IStreamVideoCallParticipant participant)
    {
        ParticipantJoined?.Invoke(participant);
    }
    
    private void OnParticipantLeft(string sessionId, string userid)
    {
        ParticipantLeft?.Invoke(sessionId);
    }

   
}