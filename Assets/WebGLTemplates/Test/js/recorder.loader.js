/***********************************************
  Unity 的处理部分
 ***********************************************/

navigator.mediaDevices.getUserMedia({ audio: true });

//全局Unity实例   （全局找 unityInstance , 然后等于它就行）
var UnityIns = null;

// 初始化
function initRecord(UnityInstance) {
  UnityIns = UnityInstance;
  recStart();
};

// 开始
function StartRecord() {
  AudiosCache.LogClear();
  //开始录音
  rec.start();
  //重置环境，开始录音时必须调用一次
  RealTimeSendTryReset();
};

// 结束
function StopRecord() {
  recStop();
};


/***********************************************
  Recorder 
  https://github.com/xiangyuecn/Recorder
 ***********************************************/

// 调用录音
var rec;
var realTimeSendTryNumber;
var transferUploadNumberMax;
var realTimeSendTryChunk;
var realTimeSendTryChunks;
var testSampleRate = 16000;
var testBitRate = 16;
/*
 每次发送指定二进制数据长度的数据帧，单位字节，16位pcm取值必须为2的整数倍，8位随意。
 16位16khz的pcm 1秒有：16000hz*16位/8比特=32000字节的数据，默认配置3200字节每秒发送大约10次
*/
var SendFrameSize = 3200;
// 开始录音
function recStart() {
  if (rec) {
    rec.close();
  }

  rec = Recorder({
    type: "unknown",
    onProcess: function (buffers, powerLevel, bufferDuration, bufferSampleRate) {
      // 推入实时处理，因为是unknown格式，buffers和rec.buffers是完全相同的，只需清理buffers就能释放内存。
      RealTimeSendTry(buffers, bufferSampleRate, false);
    }
  });

  // 打开麦克风授权获得相关资源
  rec.open(function () {//打开麦克风授权获得相关资源
    console.log("打开麦克风成功");
  }, function (msg, isUserNotAllow) {
    console.log((isUserNotAllow ? "UserNotAllow," : "") + "无法录音:" + msg);
  });
};

// 停止录音
function recStop() {
  //直接close掉即可，这个例子不需要获得最终的音频文件
  rec.stop();
  //最后一次发送
  RealTimeSendTry([], 0, true);
  audiosMergeAll();
};

//重置环境，每次开始录音时必须先调用此方法，清理环境
var RealTimeSendTryReset = function () {
  realTimeSendTryChunks = null;
};

// 实时处理
var RealTimeSendTry = function (buffers, bufferSampleRate, isClose) {
  if (realTimeSendTryChunks == null) {
    realTimeSendTryNumber = 0;
    transferUploadNumberMax = 0;
    realTimeSendTryChunk = null;
    realTimeSendTryChunks = [];
  };

  //配置有效性检查
  if (testBitRate == 16 && SendFrameSize % 2 == 1) {
    console.log("16位pcm SendFrameSize 必须为2的整数倍");
    return;
  };

  var pcm = [], pcmSampleRate = 0;
  if (buffers.length > 0) {
    //借用SampleData函数进行数据的连续处理，采样率转换是顺带的，得到新的pcm数据
    var chunk = Recorder.SampleData(buffers, bufferSampleRate, testSampleRate, realTimeSendTryChunk);

    //清理已处理完的缓冲数据，释放内存以支持长时间录音，最后完成录音时不能调用stop，因为数据已经被清掉了
    for (var i = realTimeSendTryChunk ? realTimeSendTryChunk.index : 0; i < chunk.index; i++) {
      buffers[i] = null;
    };
    //此时的chunk.data就是原始的音频16位pcm数据（小端LE），直接保存即为16位pcm文件、加个wav头即为wav文件、丢给mp3编码器转一下码即为mp3文件
    realTimeSendTryChunk = chunk;

    pcm = chunk.data;
    pcmSampleRate = chunk.sampleRate;

    //除非是onProcess给的bufferSampleRate低于testSampleRate
    if (pcmSampleRate != testSampleRate) {
      throw new Error("不应该出现pcm采样率" + pcmSampleRate + "和需要的采样率" + testSampleRate + "不一致");
    }
  };

  //将pcm数据丢进缓冲，凑够一帧发送，缓冲内的数据可能有多帧，循环切分发送
  if (pcm.length > 0) {
    realTimeSendTryChunks.push({ pcm: pcm, pcmSampleRate: pcmSampleRate });
  };

  //从缓冲中切出一帧数据
  var chunkSize = SendFrameSize / (testBitRate / 8);//8位时需要的采样数和帧大小一致，16位时采样数为帧大小的一半
  var pcm = new Int16Array(chunkSize), pcmSampleRate = 0;
  var pcmOK = false, pcmLen = 0;
  for1: for (var i1 = 0; i1 < realTimeSendTryChunks.length; i1++) {
    var chunk = realTimeSendTryChunks[i1];
    pcmSampleRate = chunk.pcmSampleRate;

    for (var i2 = chunk.offset || 0; i2 < chunk.pcm.length; i2++) {
      pcm[pcmLen] = chunk.pcm[i2];
      pcmLen++;

      //满一帧了，清除已消费掉的缓冲
      if (pcmLen == chunkSize) {
        pcmOK = true;
        chunk.offset = i2 + 1;
        for (var i3 = 0; i3 < i1; i3++) {
          realTimeSendTryChunks.splice(0, 1);
        };
        break for1;
      }
    }
  };

  //缓冲的数据不够一帧时，不发送 或者 是结束了
  if (!pcmOK) {
    if (isClose) {
      var number = ++realTimeSendTryNumber;
      TransferUpload(number, null, 0, null, isClose);
    };
    return;
  };

  //16位pcm格式可以不经过mock转码，直接发送new Blob([pcm.buffer],{type:"audio/pcm"}) 但8位的就必须转码，通用起见，均转码处理，pcm转码速度极快
  var number = ++realTimeSendTryNumber;
  var encStartTime = Date.now();
  var recMock = Recorder({
    type: "pcm",
    sampleRate: testSampleRate, //需要转换成的采样率 
    bitRate: testBitRate //需要转换成的比特率
  });

  recMock.mock(pcm, pcmSampleRate);
  recMock.stop(function (blob, duration) {
    blob.encTime = Date.now() - encStartTime;

    //转码好就推入传输
    TransferUpload(number, blob, duration, recMock, false);

    //循环调用，继续切分缓冲中的数据帧，直到不够一帧
    RealTimeSendTry([], 0, isClose);
  }, function (msg) {
    //转码错误？没想到什么时候会产生错误！
    console.log("不应该出现的错误:" + msg);
  });
};

// 数据传输函数
var TransferUpload = function (number, blobOrNull, duration, blobRec, isClose) {
  transferUploadNumberMax = Math.max(transferUploadNumberMax, number);
  if (blobOrNull) {
    var blob = blobOrNull;
    var encTime = blob.encTime;
    var numberFail = number < transferUploadNumberMax ? '<span style="color:red">顺序错乱的数据，如果要求不高可以直接丢弃</span>' : "";
    var logMsg = "No." + (number < 100 ? ("000" + number).substr(-3) : number) + numberFail;

    // 存到缓存中
    AudiosCache.LogAudio(blob, duration, blobRec);
  };

  if (isClose) {
    console.log("No." + (number < 100 ? ("000" + number).substr(-3) : number) + ":已停止传输");
  };
};

// PCM 文件合并核心函数
Recorder.PCMMerge = function (fileBytesList, bitRate, sampleRate, True, False) {
  //计算所有文件总长度
  var size = 0;
  for (var i = 0; i < fileBytesList.length; i++) {
    size += fileBytesList[i].byteLength;
  };

  //全部直接拼接到一起
  var fileBytes = new Uint8Array(size);
  var pos = 0;
  for (var i = 0; i < fileBytesList.length; i++) {
    var bytes = fileBytesList[i];
    fileBytes.set(bytes, pos);
    pos += bytes.byteLength;
  };

  //计算合并后的总时长
  var duration = Math.round(size * 8 / bitRate / sampleRate * 1000);

  True(fileBytes, duration, { bitRate: bitRate, sampleRate: sampleRate });
};

//合并日志中的所有pcm文件成一个文件
var audiosMergeAll = function () {
  var audios = AudiosCache.LogAudios;

  var bitRate = testBitRate,
    sampleRate = testSampleRate;
  var idx = -1 + 1,
    files = [],
    exclude = 0,
    badConfig = 0;
  var read = function () {
    idx++;
    if (idx >= audios.length) {
      var tips = (exclude ? "，已排除" + exclude + "个非pcm文件" : "")
        + (badConfig ? "，已排除" + badConfig + "个参数不同pcm文件" : "");
      if (!files.length) {
        console.log("至少需要录1段pcm" + tips);
        return;
      };
      Recorder.PCMMerge(files, bitRate, sampleRate, function (file, duration, info) {
        console.log("合并" + files.length + "个成功" + tips);
        info.type = "pcm";
        console.log("已经合并！");

        // 发送到 Unity 后台
        var blob = new Blob([file.buffer], { type: "audio/pcm" });
        var reader = new FileReader();
        reader.onloadend = function () {
          // 获得 base64 字符串
          var base64 = (/.+;\s*base64\s*,\s*(.+)$/i.exec(reader.result) || [])[1];
          console.log(base64);

          // 发送数据头
          if (UnityIns) {
            UnityIns.SendMessage("Recorder", "GetAudioData", base64);
          }
          else {
            alert('未初始化 Unity Instance!');
          }
        };
        reader.readAsDataURL(blob);

      }, function (msg) {
        console.log(msg + "，请清除日志后重试", 1);
      });
      return;
    };

    var logItem = audios[idx],
      logSet = logItem.set || {};
    if (!/pcm/.test(logItem.blob.type)) {
      exclude++;
      read();
      return;
    };
    if (bitRate != logSet.bitRate || sampleRate != logSet.sampleRate) {
      badConfig++;//音频参数不一致的，不合并
      read();
      return;
    };

    var reader = new FileReader();
    reader.onloadend = function () {
      files.push(new Uint8Array(reader.result));
      read();
    };
    reader.readAsArrayBuffer(logItem.blob);
  };
  read();
};