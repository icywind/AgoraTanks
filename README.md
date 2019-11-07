# AgoraTanks

In the project, we are building a game with video chat embedded to the App.  The code was based on [Unity's Tanks!!! Project](https://assetstore.unity.com/packages/essentials/tutorial-projects/tanks-reference-project-80165).  The outcome should look like the following screen in the battlefield:

![AgoraTanks-BearVsDuck](https://user-images.githubusercontent.com/1261195/68351433-53e94380-00b8-11ea-83d6-75bcc0f9a5d7.png)

The actually code may be a little different from what is posted in my Medium Blog.  However, this will be the greatest and latest version.

#### Dependencies:
- Unity 2018 LTS Recommended
- Unity Asset [Tanks!!! Reference Project](https://assetstore.unity.com/packages/essentials/tutorial-projects/tanks-reference-project-80165)
- Unity Asset [Agora Video SDK](https://assetstore.unity.com/packages/tools/video/agora-video-sdk-for-unity-134502)
- Agora AppId [Project page](https://console.agora.io/projects)

#### Project Integration:
1. After cloning this repo, you will get compiler error for missing agora references.  That's ok. Just import Agora Video SDK from Asset Store will clear it up.  
2. Open the CompleteTank prefab.  On the Plane object, attach "VideoSurface.cs" script to the missing script field.
3. Open LobbyScene
4. Make sure you have a AppId to assign to the GameSettings object.
5. Project should be runnable in Editor
6. Follow the README files from the Agora SDK for the specific platform building properties.
