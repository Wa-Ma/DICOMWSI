DICOMWSI
=======
这是DICOMWSI图像的服务端，需搭配WSI图像显示的客户端一起食用\^V^

客户端：https://github.com/Wa-Ma/WSILIB

功能：
--------
+ 提供c-find,c-store等服务
+ 存储WSI图片，等待客户端的图片请求
+ 返回查找结果，传输图片缩略图
+ 返回图片帧给客户端
+ 提供Test功能，可以在本地测试文件的传输。
+ ......
+ (老王交给你再加几点)
  
数据库：
-------
项目中mdf文件和ldf文件位数据库文件，导入sqlserver2017中即可

图片文件
-------
图片文件位于DicomWSI\DicomWSI\bin\Debug\DicomWSIStorage中

已提供了zip文件，可以在https://github.com/Wa-Ma/DICOMWSI/releases下载查看。

如要加入新的图片，课复制在此文件夹下，并添加入数据库中

如要**测试**本地文件的传输（不需要客户端）,请用提供的Test功能，将源文件放入.\Test\Source文件夹中，并指定目标文件夹（默认：.\Test\StorageFile）。

修改目标：
----------
1.将数据文件由数据库迁移至json文件来减少配置

提示：
---------
文件过大，国内用户可以先考虑转存gitlab再下载

Acknowledge：
---------
本项目有陈添大佬提供，\^V^

