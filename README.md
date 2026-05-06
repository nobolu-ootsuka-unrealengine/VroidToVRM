# VroidToVRM

-------------------------------------------------

Unity Engine 6000.4.5f1 向けです。

-------------------------------------------------

大枠の流れ

Tools の使い方

１，UnityへVRMファイルドラッグしたら

以下をするとシェーダーがVRM/MToonになります。

VRM - Shader Reapplier　プロジェクト全体

VRM - Shader Reapplier (Selected Folder)

VRM - Disable URP (All Quality Levels)

２，つぎにこのツールでシーンを作成します。

VRM - Scene Setup

３，つぎにこのツールでアニメーションを設定します。

VRM - Animation Setup

-------------------------------------------------

-------------------------------------------------

要望への対応とツール詳細。

・Unityアセットストアで取得したワールドを、気軽にUnityへインポートして使える 
　
　　Unityアセットストアには沢山素材がありすぎるのとスケールが様々なため未着手

・VRMをすぐインポートして、シェーダー適用までスムーズにできる OK

　　■ Tools > VRM - Animation Setup の使い方 
 
　　・Tools/VRM - Disable URP (All Quality Levels) すべてのレベルの Render Pipeline を None に設定してMToonを見えるようにする。
  
　　・Tools/VRM - Shader Reapplier MToonシェーダーを当てなおします プロジェクト全体
  
　　・Tools/VRM - Shader Reapplier (select Folder) MToonシェーダーを当てなおします
  
・可能なら、カメラ設定やカメラワークも手軽に操作できる

　　■ Tools > VRM - Scene Setup

　　1.VRM Prefabに　VRMのprefabをドラッグして「新規シーンを作成してセットアップ」

　　2.カメラワーク（オービット）の注目点を胴、顔に設定し、距離をドラッグすればズームインアウトできます。


・fbxやBVHなどのモーションファイルを、VRMへ手軽にリターゲティングして再生できる OK

　　■ Tools > VRM - Animation Setup の使い方
 
　　1. Animation FBX 欄 → Project ウィンドウからアニメーション FBXまたはBVH をドラッグ
  
　　2. VRM GameObject 欄 → Hierarchy から VRM の GameObject をドラッグ
  
　　3. 「一括セットアップ実行」 を押する。

-------------------------------------------------



　　
