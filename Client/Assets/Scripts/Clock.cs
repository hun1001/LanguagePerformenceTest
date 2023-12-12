using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

public class Clock : MonoBehaviour
{
    [SerializeField]
    private Text _timeText = null;

    private DateTime _startDateTime = DateTime.MinValue;
    private DateTime _endDateTime = DateTime.MaxValue;
    private TimeSpan _timeSpan = TimeSpan.Zero;

    void Update()
    {
        _timeText.text = DateTime.Now.ToString("T");
    }

    public string GetTime()
    {
        return _timeText.text;
    }

    public void StartRecord()
    {
        _startDateTime = DateTime.Now;
        _endDateTime = DateTime.MaxValue;

        StartCoroutine(nameof(RecordCoroutine));
    }

    public TimeSpan StopRecord()
    {
        StopCoroutine(nameof(RecordCoroutine));

        _endDateTime = DateTime.Now;

        return _timeSpan;
    }

    private IEnumerator RecordCoroutine()
    {
        while(true)
        {
            _timeSpan = DateTime.Now - _startDateTime;

            yield return null;
        }
    }
}
