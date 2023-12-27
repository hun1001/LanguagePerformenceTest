using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PerformancePanel : MonoBehaviour
{
    [SerializeField] private Text languageText;
    [SerializeField] private Text InformationText;

    private long _avrLatency = 0;
    private long _minLatency = 0;
    private long _maxLatency = 0;

    private long _sumLatency = 0;
    private long _recvCount = 0;

    public void Init(string language)
    {
        languageText.text = language;

        InformationText.text = $"Average: {_avrLatency} レs\r\n\r\nMin Latency: {_minLatency} レs\r\n\r\nMax Latency: {_maxLatency} レs";
    }

    public void Add(long latency)
    {
        Debug.Log( $"Add {languageText.text} Latency" );

        _sumLatency += latency;
        ++_recvCount;

        _avrLatency = _sumLatency / _recvCount;

        _minLatency = _minLatency > latency ? latency : _minLatency;
        _maxLatency = _maxLatency < latency ? latency : _maxLatency;

        InformationText.text = $"Average: {_avrLatency} レs\r\n\r\nMin Latency: {_minLatency} レs\r\n\r\nMax Latency: {_maxLatency} レs";
    }
}
