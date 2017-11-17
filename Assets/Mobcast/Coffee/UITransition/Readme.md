UITransition
===

## Overview

Transition system for uGUI.

* 単純なアニメーション(点滅、回転、拡縮など)をスクリプトやアセットなしで実装できます
* UIAnimation : シンプルなTweenを実装できます  
![image](https://user-images.githubusercontent.com/12690315/32713109-198c3952-c88b-11e7-9593-ae68d5e95cda.png)
    * Unity標準のAnimationを利用しません
    * 絶対的/相対的なTween値を設定できます
    * 以下のプロパティタイプをサポートします
        * Position
        * Rotation
        * Scale
        * Size
        * Alpha
        * (Custom)
    * 設定をプリセットとして保存、編集ができます
    * 初期位置をスクリプトから変更できます
    * Ignore Time Scaleをサポートします
    * Play On Awakeをサポートします
    * コールバックを設定できます
    * 以下のループタイプをサポートします
        * Once
        * Loop
        * Delayed Loop
        * Ping Pong
        * Delayed Ping Pong
    * 多彩なアニメーションカーブプリセットをサポートします  
![image](https://user-images.githubusercontent.com/12690315/32712713-63be8874-c889-11e7-98dc-ed7526af8884.png)
![image](https://user-images.githubusercontent.com/12690315/32712797-cbe066e8-c889-11e7-8f51-f72594bce5a8.png)
* UITransition : UIAnimationを組み合わせて、Show/Hide/Idle/Press/Clickステートに対応したアニメーションを設定できます  
![image](https://user-images.githubusercontent.com/12690315/32712849-f228ccd2-c889-11e7-9427-31eda55ef2da.png)
    * 親子構造を設定すると、複数のUITransitionを一括操作できます
    * 設定をプリセットとして保存、編集ができます
    * コールバックを設定できます
    * Advanced Option として、以下の項目を設定できます.  
    ![image](https://user-images.githubusercontent.com/12690315/32935334-13e97c46-cbb3-11e7-899e-f370f7342333.png)
        * 初期ステート
        * 追加ディレイ
        * 子に対する逐次ディレイとソート方法
        * 親から渡されたのディレイ値を無効化



## Requirement

* Unity5.4+ *(included Unity 2017.x)*
* No other SDK is required.




## Usage

1. Download [UITransition.unitypackage](https://github.com/mob-sakai/UITransition/raw/develop/UITransition.unitypackage) and install on your unity project.
1. AddComponent `UITransition` or `UIAnimation` to the GameObject.
1. Enjoy!




## Demo

![](https://user-images.githubusercontent.com/12690315/32713085-f983ab4a-c88a-11e7-8492-cb7362365132.gif)




## Release Notes

### ver.1.1.0:
* ![image](https://user-images.githubusercontent.com/12690315/32935334-13e97c46-cbb3-11e7-899e-f370f7342333.png)
* Feature: Changing state on enable.
* Feature: Delay children sequencial.
    * Position X/Y
    * Hierarchy
    * Reversing

### ver.1.0.0:
* Feature: UITransition
    * Supports Show/Hide/Idle/Press/Click states.
    * Parent-child relations for bulk operation.
    * Editable preset.
    * Additional delaying on Show/Hide.
    * Ignore Time Scale.
* Feature: UIAnimation
    * Supports simple property tweening.
    * Absolute/Relative tween value.
    * Editable preset.
    * Animation curve.
    * Looping.
    * Play on awake.
    * Ignore time scale.
    * Play forward/reverse.




## See Also

* GitHub Page : https://github.com/mob-sakai/UITransition
* Issue tracker : https://github.com/mob-sakai/UITransition/issues