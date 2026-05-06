# VRMToUnity3D

-------------------------------------------------

VRM0.0向け

Unity Engine 6000.4.5f1 向けです。

-------------------------------------------------

# 大枠の流れ

０，Download ZIPを展開したらUnityHubで「Add」で以下のフォルダを選びます。

D:\Download\VroidToVRM-main\VroidToVRM-main\VroidToVRM\VRMtoUnity3D

# Tools の使い方

<img width="320" height="138" alt="Image" src="https://github.com/user-attachments/assets/f1b0dc3a-539a-4fd1-ab64-5446f6da99d5" />

# １，UnityへVRMファイルドラッグしたら

以下をするとシェーダーがVRM/MToonになります。

VRM - Shader Reapplier　プロジェクト全体

VRM - Shader Reapplier (Selected Folder)

VRM - Disable URP (All Quality Levels)

# ２，つぎにこのツールでシーンを作成します。

VRM - Scene Setup

# ３，つぎにこのツールでアニメーションを設定します。

VRM - Animation Setup

-------------------------------------------------

-------------------------------------------------

# 要望への対応とツール詳細。

# ・Unityアセットストアで取得したワールドを、気軽にUnityへインポートして使える 
　
　　Unityアセットストアには沢山素材がありすぎるのとスケールが様々なため未着手

# ・VRMをすぐインポートして、シェーダー適用までスムーズにできる OK

　　■ Tools > VRM - Animation Setup の使い方 
 
　　・Tools/VRM - Shader Reapplier MToonシェーダーを当てなおします プロジェクト全体

<img width="333" height="442" alt="Image" src="https://github.com/user-attachments/assets/83b80f46-0d12-4225-a272-cb0f958fd7cd" />
  
　　・Tools/VRM - Shader Reapplier (select Folder) MToonシェーダーを当てなおします
 
　　・Tools/VRM - Disable URP (All Quality Levels) すべてのレベルの Render Pipeline を None に設定してMToonを見えるようにする。
 
# ・可能なら、カメラ設定やカメラワークも手軽に操作できる

　　■ Tools > VRM - Scene Setup

<img width="321" height="531" alt="Image" src="https://github.com/user-attachments/assets/16533f93-f8af-42e5-a67f-cf14839a8652" />

　　1.VRM Prefabに　VRMのprefabをドラッグして「新規シーンを作成してセットアップ」

　　2.カメラワーク（オービット）の注目点を胴、顔に設定し、距離をドラッグすればズームインアウトできます。


# ・fbxやBVHなどのモーションファイルを、VRMへ手軽にリターゲティングして再生できる OK

　　■ Tools > VRM - Animation Setup の使い方

<img width="323" height="297" alt="Image" src="https://github.com/user-attachments/assets/42823be5-1d2d-4e6c-9552-f1756545889c" />
 
　　1. Animation FBX 欄 → Project ウィンドウからアニメーション FBXまたはBVH をドラッグ
  
　　2. VRM GameObject 欄 → Hierarchy から VRM の GameObject をドラッグ
  
　　3. 「一括セットアップ実行」 を押する。

-------------------------------------------------



　　
