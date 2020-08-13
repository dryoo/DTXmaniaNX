﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Diagnostics;
using SlimDX.Direct3D9;
using FDK;

namespace DTXMania
{
	internal class CStagePlayingScreenGuitar : CStagePlayingScreenCommon
	{
		// コンストラクタ

		public CStagePlayingScreenGuitar()
		{
			base.eステージID = CStage.EStage.Playing;
			base.eフェーズID = CStage.Eフェーズ.共通_通常状態;
			base.bNotActivated = true;
			base.listChildActivities.Add( this.actStageFailed = new CAct演奏ステージ失敗() );
			base.listChildActivities.Add( this.actDANGER = new CAct演奏GuitarDanger() );
			base.listChildActivities.Add( this.actAVI = new CAct演奏AVI() );
			base.listChildActivities.Add( this.actBGA = new CAct演奏BGA() );
            base.listChildActivities.Add( this.actGraph = new CAct演奏スキルメーター() );
//			base.listChildActivities.Add( this.actPanel = new CAct演奏パネル文字列() );
			base.listChildActivities.Add( this.act譜面スクロール速度 = new CAct演奏スクロール速度() );
			base.listChildActivities.Add( this.actStatusPanels = new CAct演奏Guitarステータスパネル() );
			base.listChildActivities.Add( this.actWailingBonus = new CAct演奏GuitarWailingBonus() );
			base.listChildActivities.Add( this.actScore = new CAct演奏Guitarスコア() );
			base.listChildActivities.Add( this.actRGB = new CAct演奏GuitarRGB() );
			base.listChildActivities.Add( this.actLaneFlushGB = new CAct演奏GuitarレーンフラッシュGB() );
			base.listChildActivities.Add( this.actJudgeString = new CAct演奏Guitar判定文字列() );
			base.listChildActivities.Add( this.actGauge = new CAct演奏Guitarゲージ() );
			base.listChildActivities.Add( this.actCombo = new CAct演奏Guitarコンボ() );
			base.listChildActivities.Add( this.actChipFireGB = new CAct演奏Guitarチップファイア() );
			base.listChildActivities.Add( this.actPlayInfo = new CActPerformanceInformation() );
			base.listChildActivities.Add( this.actFI = new CActFIFOBlackStart() );
			base.listChildActivities.Add( this.actFO = new CActFIFOBlack() );
			base.listChildActivities.Add( this.actFOClear = new CActFIFOWhite() );
            base.listChildActivities.Add( this.actFOStageClear = new CActFIFOWhiteClear());
		}


		// メソッド

		public void t演奏結果を格納する( out CScoreIni.CPerformanceEntry Drums, out CScoreIni.CPerformanceEntry Guitar, out CScoreIni.CPerformanceEntry Bass )
		{
			Drums = new CScoreIni.CPerformanceEntry();

			base.tStorePerfResults_Guitar( out Guitar );
			base.tStorePerfResultsBass( out Bass );

//			if ( CDTXMania.ConfigIni.bIsSwappedGuitarBass )		// #24063 2011.1.24 yyagi Gt/Bsを入れ替えていたなら、演奏結果も入れ替える
//			{
//				CScoreIni.CPerformanceEntry t;
//				t = Guitar;
//				Guitar = Bass;
//				Bass = t;
//			
//				CDTXMania.DTX.SwapGuitarBassInfos();			// 譜面情報も元に戻す
//			}
		}
		

		// CStage 実装

		public override void OnActivate()
		{
            int nGraphUsePart = CDTXMania.ConfigIni.bGraph有効.Guitar ? 1 : 2;
            this.ct登場用 = new CCounter(0, 12, 16, CDTXMania.Timer);
            dtLastQueueOperation = DateTime.MinValue;
            if( CDTXMania.bCompactMode )
            {
                var score = new CScore();
                CDTXMania.SongManager.tScoreIniを読み込んで譜面情報を設定する(CDTXMania.strCompactModeFile + ".score.ini", ref score);
                this.actGraph.dbグラフ値目標_渡 = score.SongInformation.HighSkill[ nGraphUsePart ];
            }
            else
            {
				this.actGraph.dbグラフ値目標_渡 = CDTXMania.stageSongSelection.r確定されたスコア.SongInformation.HighSkill[ nGraphUsePart ];	// #24074 2011.01.23 add ikanick
                this.actGraph.dbグラフ値自己ベスト = CDTXMania.stageSongSelection.r確定されたスコア.SongInformation.HighSkill[ nGraphUsePart ];

                // #35411 2015.08.21 chnmr0 add
                // ゴースト利用可のなとき、0で初期化
                if (CDTXMania.ConfigIni.eTargetGhost[ nGraphUsePart ] != ETargetGhostData.NONE)
                {
                    if (CDTXMania.listTargetGhsotLag[ nGraphUsePart ] != null)
                    {
                        this.actGraph.dbグラフ値目標_渡 = 0;
                    }
                }
            }
			base.OnActivate();
		}
		public override void OnManagedCreateResources()
		{
			if( !base.bNotActivated )
			{
                this.bサビ区間 = false;
				//this.tGenerateBackgroundTexture();
				this.txチップ = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\7_Chips_Guitar.png" ) );
                this.txレーン = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\7_lanes_Guitar.png") );
                this.txヒットバー = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\\ScreenPlayDrums hit-bar.png"));
				//this.txWailing枠 = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\ScreenPlay wailing cursor.png" ) );
				base.OnManagedCreateResources();
			}
		}
		public override void OnManagedReleaseResources()
		{
			if( !base.bNotActivated )
			{
				//CDTXMania.tReleaseTexture( ref this.txBackground );
				CDTXMania.tReleaseTexture( ref this.txチップ );
                CDTXMania.tReleaseTexture( ref this.txレーン );
				CDTXMania.tReleaseTexture( ref this.txヒットバー );
				//CDTXMania.tReleaseTexture( ref this.txWailing枠 );
				base.OnManagedReleaseResources();
			}
		}
		public override int OnUpdateAndDraw()
		{
			if( !base.bNotActivated )
			{
                bool flag = false;
                bool flag2 = false;

				if( base.bJustStartedUpdate )
				{
                    CSoundManager.rc演奏用タイマ.tリセット();
					CDTXMania.Timer.tリセット();

                    this.UnitTime = ((60.0 / (CDTXMania.stagePlayingScreenGuitar.actPlayInfo.dbBPM) / 14.0)); //2014.01.14.kairera0467 これも動かしたいのだが____

					this.ctチップ模様アニメ.Guitar = new CCounter( 0, 0x17, 20, CDTXMania.Timer );
					this.ctチップ模様アニメ.Bass = new CCounter( 0, 0x17, 20, CDTXMania.Timer );
					this.ctチップ模様アニメ[ 0 ] = null;
                    this.ctコンボ動作タイマ = new CCounter(1, 16, (int)((60.0 / (CDTXMania.stagePlayingScreenGuitar.actPlayInfo.dbBPM) / 16.0 * 1000.0)), CDTXMania.Timer);
					this.ctWailingチップ模様アニメ = new CCounter( 0, 4, 50, CDTXMania.Timer );

                    if( this.tx判定画像anime != null && this.txボーナスエフェクト != null )
                    {
                        this.tx判定画像anime.tDraw2D( CDTXMania.app.Device, 1280, 720 );
                        this.txボーナスエフェクト.tDraw2D( CDTXMania.app.Device, 1280, 720 );
                    }
					base.eフェーズID = CStage.Eフェーズ.共通_フェードイン;
					this.actFI.tフェードイン開始();
					base.bJustStartedUpdate = false;
				}
				if( CDTXMania.ConfigIni.bSTAGEFAILED有効 && ( base.eフェーズID == CStage.Eフェーズ.共通_通常状態 ) )
				{
					bool bFailedGuitar = this.actGauge.IsFailed( EInstrumentPart.GUITAR );		// #23630 2011.11.12 yyagi: deleted AutoPlay condition: not to be failed at once
					bool bFailedBass   = this.actGauge.IsFailed( EInstrumentPart.BASS );		// #23630
					bool bFailedNoChips = (!CDTXMania.DTX.bチップがある.Guitar && !CDTXMania.DTX.bチップがある.Bass);	// #25216 2011.5.21 yyagi add condition
					if ( bFailedGuitar || bFailedBass || bFailedNoChips )						// #25216 2011.5.21 yyagi: changed codition: && -> ||
					{
						this.actStageFailed.Start();
						CDTXMania.DTX.t全チップの再生停止();
						base.eフェーズID = CStage.Eフェーズ.演奏_STAGE_FAILED;
					}
				}
				this.t進行描画_背景();
                this.t進行描画_AVI();
				this.t進行描画_MIDIBGM();

//                if (CDTXMania.ConfigIni.bShowMusicInfo)
//				    this.t進行描画_パネル文字列();

				this.t進行描画_レーンフラッシュGB();

				this.t進行描画_DANGER();

				this.t進行描画_WailingBonus();
				this.t進行描画_譜面スクロール速度();
				this.t進行描画_チップアニメ();
                this.t進行描画_小節線(EInstrumentPart.GUITAR);
                flag = this.t進行描画_チップ(EInstrumentPart.GUITAR);
                this.t進行描画_RGBボタン();
                this.t進行描画_ギターベース判定ライン();
				this.t進行描画_判定文字列();
                this.t進行描画_ゲージ();
                if (CDTXMania.ConfigIni.nInfoType == 1)
				    this.t進行描画_ステータスパネル();
                if (CDTXMania.ConfigIni.bShowScore)
                    this.t進行描画_スコア();
                this.t進行描画_グラフ();
                this.t進行描画_コンボ();
				this.t進行描画_演奏情報();
				//this.t進行描画_Wailing枠();

                this.t進行描画_チップファイアGB();
				this.t進行描画_STAGEFAILED();
                flag2 = this.t進行描画_フェードイン_アウト();
                if ( flag && (base.eフェーズID == CStage.Eフェーズ.共通_通常状態 ) )
                {
                    this.eフェードアウト完了時の戻り値 = E演奏画面の戻り値.ステージクリア;
                    base.eフェーズID = CStage.Eフェーズ.演奏_STAGE_CLEAR_フェードアウト;
                    this.actFOStageClear.tフェードアウト開始();
                }
				if( flag2 )
				{
                    if (!CDTXMania.Skin.soundステージクリア音.b再生中)
                    {
                        Debug.WriteLine("Total OnUpdateAndDraw=" + sw.ElapsedMilliseconds + "ms");
                        return (int)this.eフェードアウト完了時の戻り値;
                    }
				}
                ManageMixerQueue();

				// キー入力

				if( CDTXMania.act現在入力を占有中のプラグイン == null )
				{
					this.tキー入力();
				}
			}
            base.sw.Stop();
			return 0;
		}


		// その他

		#region [ private ]
		//-----------------
        private CTexture txレーン;
        public bool bサビ区間;
        public double UnitTime;

		protected override E判定 tチップのヒット処理( long nHitTime, CDTX.CChip pChip, bool bCorrectLane )
		{
			E判定 eJudgeResult = tチップのヒット処理( nHitTime, pChip, EInstrumentPart.GUITAR, bCorrectLane );
            if( pChip.e楽器パート == EInstrumentPart.GUITAR && CDTXMania.ConfigIni.bGraph有効.Guitar )
            {
                if( CDTXMania.ConfigIni.nSkillMode == 0 )
			        this.actGraph.dbグラフ値現在_渡 = CScoreIni.tCalculatePlayingSkillOld( CDTXMania.DTX.n可視チップ数.Guitar, this.nヒット数_Auto含まない.Guitar.Perfect, this.nヒット数_Auto含まない.Guitar.Great, this.nヒット数_Auto含まない.Guitar.Good, this.nヒット数_Auto含まない.Guitar.Poor, this.nヒット数_Auto含まない.Guitar.Miss, this.actCombo.n現在のコンボ数.最高値.Guitar, EInstrumentPart.GUITAR,  bIsAutoPlay );
                else
	    		    this.actGraph.dbグラフ値現在_渡 = CScoreIni.tCalculatePlayingSkill( CDTXMania.DTX.n可視チップ数.Guitar, this.nヒット数_Auto含まない.Guitar.Perfect, this.nヒット数_Auto含まない.Guitar.Great, this.nヒット数_Auto含まない.Guitar.Good, this.nヒット数_Auto含まない.Guitar.Poor, this.nヒット数_Auto含まない.Guitar.Miss, this.actCombo.n現在のコンボ数.最高値.Guitar, EInstrumentPart.GUITAR,  bIsAutoPlay );

		    	if( CDTXMania.listTargetGhsotLag.Guitar != null &&
                    CDTXMania.ConfigIni.eTargetGhost.Guitar == ETargetGhostData.ONLINE &&
				    CDTXMania.DTX.n可視チップ数.Guitar > 0 )
    			{

	    			this.actGraph.dbグラフ値現在_渡 = 100 *
		    						(this.nヒット数_Auto含まない.Guitar.Perfect * 17 +
			    					 this.nヒット数_Auto含まない.Guitar.Great * 7 +
				    				 this.actCombo.n現在のコンボ数.最高値.Guitar * 3) / (20.0 * CDTXMania.DTX.n可視チップ数.Guitar );
    			}

                this.actGraph.n現在のAutoを含まない判定数_渡[ 0 ] = this.nヒット数_Auto含まない.Guitar.Perfect;
                this.actGraph.n現在のAutoを含まない判定数_渡[ 1 ] = this.nヒット数_Auto含まない.Guitar.Great;
                this.actGraph.n現在のAutoを含まない判定数_渡[ 2 ] = this.nヒット数_Auto含まない.Guitar.Good;
                this.actGraph.n現在のAutoを含まない判定数_渡[ 3 ] = this.nヒット数_Auto含まない.Guitar.Poor;
                this.actGraph.n現在のAutoを含まない判定数_渡[ 4 ] = this.nヒット数_Auto含まない.Guitar.Miss;
            }
            else if( pChip.e楽器パート == EInstrumentPart.BASS && CDTXMania.ConfigIni.bGraph有効.Bass )
            {
                if( CDTXMania.ConfigIni.nSkillMode == 0 )
			        this.actGraph.dbグラフ値現在_渡 = CScoreIni.tCalculatePlayingSkillOld( CDTXMania.DTX.n可視チップ数.Bass, this.nヒット数_Auto含まない.Bass.Perfect, this.nヒット数_Auto含まない.Bass.Great, this.nヒット数_Auto含まない.Bass.Good, this.nヒット数_Auto含まない.Bass.Poor, this.nヒット数_Auto含まない.Bass.Miss, this.actCombo.n現在のコンボ数.最高値.Bass, EInstrumentPart.BASS,  bIsAutoPlay );
                else
	    		    this.actGraph.dbグラフ値現在_渡 = CScoreIni.tCalculatePlayingSkill( CDTXMania.DTX.n可視チップ数.Bass, this.nヒット数_Auto含まない.Bass.Perfect, this.nヒット数_Auto含まない.Bass.Great, this.nヒット数_Auto含まない.Bass.Good, this.nヒット数_Auto含まない.Bass.Poor, this.nヒット数_Auto含まない.Bass.Miss, this.actCombo.n現在のコンボ数.最高値.Bass, EInstrumentPart.BASS,  bIsAutoPlay );

		    	if( CDTXMania.listTargetGhsotLag.Bass != null &&
                    CDTXMania.ConfigIni.eTargetGhost.Bass == ETargetGhostData.ONLINE &&
				    CDTXMania.DTX.n可視チップ数.Bass > 0 )
    			{

	    			this.actGraph.dbグラフ値現在_渡 = 100 *
		    						(this.nヒット数_Auto含まない.Bass.Perfect * 17 +
			    					 this.nヒット数_Auto含まない.Bass.Great * 7 +
				    				 this.actCombo.n現在のコンボ数.最高値.Bass * 3) / (20.0 * CDTXMania.DTX.n可視チップ数.Bass );
    			}

                this.actGraph.n現在のAutoを含まない判定数_渡[ 0 ] = this.nヒット数_Auto含まない.Bass.Perfect;
                this.actGraph.n現在のAutoを含まない判定数_渡[ 1 ] = this.nヒット数_Auto含まない.Bass.Great;
                this.actGraph.n現在のAutoを含まない判定数_渡[ 2 ] = this.nヒット数_Auto含まない.Bass.Good;
                this.actGraph.n現在のAutoを含まない判定数_渡[ 3 ] = this.nヒット数_Auto含まない.Bass.Poor;
                this.actGraph.n現在のAutoを含まない判定数_渡[ 4 ] = this.nヒット数_Auto含まない.Bass.Miss;
            }
			return eJudgeResult;
		}
		protected override void tチップのヒット処理_BadならびにTight時のMiss( EInstrumentPart part )
		{
			this.tチップのヒット処理_BadならびにTight時のMiss( part, 0, EInstrumentPart.GUITAR );
		}
		protected override void tチップのヒット処理_BadならびにTight時のMiss( EInstrumentPart part, int nLane )
		{
			this.tチップのヒット処理_BadならびにTight時のMiss( part, nLane, EInstrumentPart.GUITAR );
		}

        /*
		protected override void t進行描画_AVI()
		{
		    base.t進行描画_AVI( 0, 0 );
		}
		protected override void t進行描画_BGA()
		{
		    base.t進行描画_BGA( 500, 50 );
		}
         */
		protected override void t進行描画_DANGER()			// #23631 2011.4.19 yyagi
		{
			//this.actDANGER.t進行描画( false, this.actGauge.db現在のゲージ値.Guitar < 0.3, this.actGauge.db現在のゲージ値.Bass < 0.3 );
			this.actDANGER.t進行描画( false, this.actGauge.IsDanger(EInstrumentPart.GUITAR), this.actGauge.IsDanger(EInstrumentPart.BASS) );
		}
        private void t進行描画_グラフ()
        {
			if ( !CDTXMania.ConfigIni.bストイックモード && ( CDTXMania.ConfigIni.bGraph有効.Guitar || CDTXMania.ConfigIni.bGraph有効.Bass ) )
			{
                this.actGraph.OnUpdateAndDraw();
            }
        }
		protected override void t進行描画_Wailing枠()
		{
			base.t進行描画_Wailing枠( 292, 0x251,
				CDTXMania.ConfigIni.bReverse.Guitar ? 340 : 130,
				CDTXMania.ConfigIni.bReverse.Bass ?   340 : 130
			);
		}
		private void t進行描画_ギターベース判定ライン()	// yyagi: ドラム画面とは座標が違うだけですが、まとめづらかったのでそのまま放置してます。
		{
			if ( CDTXMania.ConfigIni.bGuitar有効 )
			{
				if ( CDTXMania.DTX.bチップがある.Guitar )
				{
                    int y = CDTXMania.ConfigIni.bReverse.Guitar ? this.nJudgeLinePosY.Guitar : this.nJudgeLinePosY.Guitar - 1;

						if ( this.txヒットバー != null && CDTXMania.ConfigIni.bJudgeLineDisp.Guitar )
							this.txヒットバー.tDraw2D( CDTXMania.app.Device, 80, y, new Rectangle( 0, 0, 252, 6 ) );

                    if (CDTXMania.ConfigIni.b演奏情報を表示する)
                        this.actLVFont.t文字列描画(310, (CDTXMania.ConfigIni.bReverse.Guitar ? y + 8 : y - 20), CDTXMania.ConfigIni.nJudgeLine.Guitar.ToString());
				}
				if ( CDTXMania.DTX.bチップがある.Bass )
				{
                    int y = CDTXMania.ConfigIni.bReverse.Bass ? this.nJudgeLinePosY.Bass : this.nJudgeLinePosY.Bass - 1;

						if ( this.txヒットバー != null && CDTXMania.ConfigIni.bJudgeLineDisp.Bass )
                            this.txヒットバー.tDraw2D(CDTXMania.app.Device, 950, y, new Rectangle(0, 0, 252, 6));

                    if (CDTXMania.ConfigIni.b演奏情報を表示する)
                        this.actLVFont.t文字列描画(1180, (CDTXMania.ConfigIni.bReverse.Bass ? y + 8 : y - 20), CDTXMania.ConfigIni.nJudgeLine.Bass.ToString());
                }
			}
		}

        /*
		protected override void t進行描画_パネル文字列()
		{
			base.t進行描画_パネル文字列( 0xb5, 430 );
		}
         */

		protected override void t進行描画_演奏情報()
		{
			base.t進行描画_演奏情報( 500, 257 );
		}

        protected override void tJudgeLineMovingUpandDown()
        {

        }

		protected override void ドラムスクロール速度アップ()
		{
            CDTXMania.ConfigIni.n譜面スクロール速度.Guitar = Math.Min(CDTXMania.ConfigIni.n譜面スクロール速度.Guitar + 1, 1999);
		}
		protected override void ドラムスクロール速度ダウン()
		{
            CDTXMania.ConfigIni.n譜面スクロール速度.Guitar = Math.Max(CDTXMania.ConfigIni.n譜面スクロール速度.Guitar - 1, 0);
		}

		protected override void t入力処理_ドラム()
		{
			// ギタレボモードでは何もしない
		}

		protected override void tGenerateBackgroundTexture()
		{
			Rectangle bgrect = new Rectangle( 0, 0, 1280, 720 );
			string DefaultBgFilename = @"Graphics\7_background_Guitar.jpg";
			string BgFilename = "";
			string BACKGROUND = null;
			if ( ( CDTXMania.DTX.BACKGROUND_GR != null ) && ( CDTXMania.DTX.BACKGROUND_GR.Length > 0 ) )
			{
				BACKGROUND = CDTXMania.DTX.BACKGROUND_GR;
			}
			else if ( ( CDTXMania.DTX.BACKGROUND != null ) && ( CDTXMania.DTX.BACKGROUND.Length > 0 ) )
			{
				BACKGROUND = CDTXMania.DTX.BACKGROUND;
			}
			if ( ( BACKGROUND != null ) && ( BACKGROUND.Length > 0 ) )
			{
				BgFilename = CDTXMania.DTX.strフォルダ名 + BACKGROUND;
			}
			base.tGenerateBackgroundTexture( DefaultBgFilename, bgrect, BgFilename );
		}
        protected override void t進行描画_チップ_模様のみ_ドラムス(CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip)
        {
            // int indexSevenLanes = this.nチャンネル0Atoレーン07[ pChip.nチャンネル番号 - 0x11 ];
            if (!pChip.bHit && (pChip.nバーからの距離dot.Drums < 0))
            {
                //pChip.bHit = true;
                //this.tサウンド再生(pChip, CSoundManager.rc演奏用タイマ.n前回リセットした時のシステム時刻 + pChip.n発声時刻ms, EInstrumentPart.DRUMS, dTX.nモニタを考慮した音量(EInstrumentPart.DRUMS));
            }
        }
        protected override void t進行描画_チップ_ドラムス(CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip)
		{
			// int indexSevenLanes = this.nチャンネル0Atoレーン07[ pChip.nチャンネル番号 - 0x11 ];
			if ( !pChip.bHit && ( pChip.nバーからの距離dot.Drums < 0 ) )
			{
				pChip.bHit = true;
                this.tサウンド再生(pChip, CSoundManager.rc演奏用タイマ.n前回リセットした時のシステム時刻 + pChip.n発声時刻ms, EInstrumentPart.DRUMS, dTX.nモニタを考慮した音量(EInstrumentPart.DRUMS));
			}
		}
		protected override void t進行描画_チップ_ギターベース( CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip, EInstrumentPart inst )
		{
			base.t進行描画_チップ_ギターベース( configIni, ref dTX, ref pChip, inst,
                this.nJudgeLinePosY[ (int) inst ] + 10, this.nJudgeLinePosY[ (int) inst ] + 1, 104, 670, 0, 0, 0, 11, 196, 10, 38, 38, 1000, 1000, 1000, 38, 38);
		}
#if false
		protected override void t進行描画_チップ_ギターベース( CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip, E楽器パート inst )
		{
			int instIndex = (int) inst;
			if ( configIni.bGuitar有効 )
			{
				if ( configIni.bSudden[instIndex ] )
				{
					pChip.b可視 = pChip.nバーからの距離dot[ instIndex ] < 200;
				}
				if ( configIni.bHidden[ instIndex ] && ( pChip.nバーからの距離dot[ instIndex ] < 100 ) )
				{
					pChip.b可視 = false;
				}

				bool bChipHasR = ( ( pChip.nチャンネル番号 & 4 ) > 0 );
				bool bChipHasG = ( ( pChip.nチャンネル番号 & 2 ) > 0 );
				bool bChipHasB = ( ( pChip.nチャンネル番号 & 1 ) > 0 );
				bool bChipHasW = ( ( pChip.nチャンネル番号 & 0x0F ) == 0x08 );
				bool bChipIsO  = ( ( pChip.nチャンネル番号 & 0x0F ) == 0x00 );

				int OPEN = ( inst == E楽器パート.GUITAR ) ? 0x20 : 0xA0;
				if ( !pChip.bHit && pChip.b可視 )
				{
					int y = configIni.bReverse[ instIndex ] ? ( 369 - pChip.nバーからの距離dot[ instIndex ]) : ( 40 + pChip.nバーからの距離dot[ instIndex ] );
					if ( ( y > 0 ) && ( y < 409 ) )
					{
						if ( this.txチップ != null )
						{
							int nアニメカウンタ現在の値 = this.ctチップ模様アニメ[ instIndex ].n現在の値;
							if ( pChip.nチャンネル番号 == OPEN )
							{
								{
									int xo = ( inst == E楽器パート.GUITAR ) ? 26 : 480;
									this.txチップ.t2D描画( CDTXMania.app.Device, xo, y - 4, new Rectangle( 0, 192 + ( ( nアニメカウンタ現在の値 % 5 ) * 8 ), 103, 8 ) );
								}
							}
							Rectangle rc = new Rectangle( 0, nアニメカウンタ現在の値 * 8, 32, 8 );
							int x;
							if ( inst == E楽器パート.GUITAR )
							{
								x = ( configIni.bLeft.Guitar ) ? 98 : 26;
							}
							else
							{
								x = ( configIni.bLeft.Bass ) ? 552 : 480;
							}
							int deltaX = ( configIni.bLeft[ instIndex ] ) ? -36 : +36; 
							if ( bChipHasR )
							{
								this.txチップ.t2D描画( CDTXMania.app.Device, x, y - 4, rc );
							}
							rc.X += 32;
							if ( bChipHasG )
							{
								this.txチップ.t2D描画( CDTXMania.app.Device, x, y - 4, rc );
							}
							rc.X += 32;
							if ( bChipHasB )
							{
								this.txチップ.t2D描画( CDTXMania.app.Device, x, y - 4, rc );
							}
						}
					}
				}
				// if ( ( configIni.bAutoPlay.Guitar && !pChip.bHit ) && ( pChip.nバーからの距離dot.Guitar < 0 ) )
				if ( ( !pChip.bHit ) && ( pChip.nバーからの距離dot[ instIndex ] < 0 ) )
				{
					int lo = ( inst == E楽器パート.GUITAR ) ? 0 : 3;	// lane offset
					bool autoR = ( inst == E楽器パート.GUITAR ) ? bIsAutoPlay.GtR : bIsAutoPlay.BsR;
					bool autoG = ( inst == E楽器パート.GUITAR ) ? bIsAutoPlay.GtG : bIsAutoPlay.BsG;
					bool autoB = ( inst == E楽器パート.GUITAR ) ? bIsAutoPlay.GtB : bIsAutoPlay.BsB;
					if ( ( bChipHasR || bChipIsO ) && autoR )
					{
						this.actChipFireGB.Start( 0 + lo );
					}
					if ( ( bChipHasG || bChipIsO ) && autoG )
					{
						this.actChipFireGB.Start( 1 + lo );
					}
					if ( ( bChipHasB || bChipIsO ) && autoB )
					{
						this.actChipFireGB.Start( 2 + lo );
					}
					if ( ( inst == E楽器パート.GUITAR && bIsAutoPlay.GtPick ) || ( inst == E楽器パート.BASS && bIsAutoPlay.BsPick ) )
					{
						bool pushingR = CDTXMania.Pad.b押されている( inst, Eパッド.R );
						bool pushingG = CDTXMania.Pad.b押されている( inst, Eパッド.G );
						bool pushingB = CDTXMania.Pad.b押されている( inst, Eパッド.B );
						bool bMiss = true;
						if ( ( ( bChipIsO == true ) && ( !pushingR | autoR ) && ( !pushingG | autoG ) && ( !pushingB | autoB ) ) ||
							( ( bChipHasR == ( pushingR | autoR ) ) && ( bChipHasG == ( pushingG | autoG ) ) && ( bChipHasB == ( pushingB | autoB ) ) )
						)
						{
							bMiss = false;
						}
						pChip.bHit = true;
						this.tサウンド再生( pChip, CDTXMania.Timer.n前回リセットした時のシステム時刻 + pChip.n発声時刻ms, inst, dTX.nモニタを考慮した音量( inst ) );
						this.r次にくるギターChip = null;
						this.tチップのヒット処理( pChip.n発声時刻ms, pChip );
					}
				}
				// break;
				return;
			}
			if ( !pChip.bHit && ( pChip.nバーからの距離dot[ instIndex ] < 0 ) )
			{
				pChip.bHit = true;
				this.tサウンド再生( pChip, CDTXMania.Timer.n前回リセットした時のシステム時刻 + pChip.n発声時刻ms, inst, dTX.nモニタを考慮した音量( inst ) );
			}
		}
#endif
		protected override void t進行描画_チップ_ギター_ウェイリング( CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip )
		{
			if ( configIni.bGuitar有効 )
			{
				//if ( configIni.bSudden.Guitar )
				//{
				//    pChip.b可視 = pChip.nバーからの距離dot.Guitar < 200;
				//}
				//if ( configIni.bHidden.Guitar && ( pChip.nバーからの距離dot.Guitar < 100 ) )
				//{
				//    pChip.b可視 = false;
				//}

				//
				// 後日、以下の部分を何とかCStage演奏画面共通.csに移したい。
				//
				if ( !pChip.bHit && pChip.b可視 )
				{
					int[] y_base = { 154, 611 };			// ドラム画面かギター画面かで変わる値
					int offset = 0;						// ドラム画面かギター画面かで変わる値

					const int WailingWidth = 54;		// 4種全て同じ値
					const int WailingHeight = 68;		// 4種全て同じ値
					const int baseTextureOffsetX = 0;	// ドラム画面かギター画面かで変わる値
					const int baseTextureOffsetY = 22;	// ドラム画面かギター画面かで変わる値
					const int drawX = 287;				// 4種全て異なる値

					const int numA = 34;				// 4種全て同じ値;
					int y = configIni.bReverse.Guitar ? ( y_base[ 1 ] - pChip.nバーからの距離dot.Guitar ) : ( y_base[ 0 ] + pChip.nバーからの距離dot.Guitar );
					int numB = y - offset;				// 4種全て同じ定義
					int numC = 0;						// 4種全て同じ初期値
					const int numD = 709;				// ドラム画面かギター画面かで変わる値
					if ( ( numB < ( numD + numA ) ) && ( numB > -numA ) )	// 以下のロジックは4種全て同じ
					{
						int c = this.ctWailingチップ模様アニメ.n現在の値;
						Rectangle rect = new Rectangle( baseTextureOffsetX, baseTextureOffsetY, WailingWidth, WailingHeight );
						if ( numB < numA )
						{
							rect.Y += numA - numB;
							rect.Height -= numA - numB;
							numC = numA - numB;
						}
						if ( numB > ( numD - numA ) )
						{
							rect.Height -= numB - ( numD - numA );
						}
						if ( ( rect.Bottom > rect.Top ) && ( this.txチップ != null ) )
						{
							this.txチップ.tDraw2D( CDTXMania.app.Device, drawX, ( y - numA ) + numC, rect );
						}
					}
				}
				//    if( !pChip.bHit && ( pChip.nバーからの距離dot.Guitar < 0 ) )
				//    {
				//        pChip.bHit = true;
				//        if( configIni.bAutoPlay.Guitar )
				//        {
				//            this.actWailingBonus.Start( EInstrumentPart.GUITAR, this.r現在の歓声Chip.Guitar );
				//        }
				//    }
				//    return;
				//}
				//pChip.bHit = true;
			}
			base.t進行描画_チップ_ギター_ウェイリング( configIni, ref dTX, ref pChip );
		}
		protected override void t進行描画_チップ_フィルイン( CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip )
		{
			if ( !pChip.bHit && ( pChip.nバーからの距離dot.Drums < 0 ) )
			{
				pChip.bHit = true;
			}
#if TEST_NOTEOFFMODE	// 2011.1.1 yyagi TEST
			switch ( pChip.n整数値 )
			{
				case 0x04:	// HH消音あり(従来同等)
					CDTXMania.DTX.b演奏で直前の音を消音する.HH = true;
					break;
				case 0x05:	// HH消音無し
					CDTXMania.DTX.b演奏で直前の音を消音する.HH = false;
					break;
				case 0x06:	// ギター消音あり(従来同等)
					CDTXMania.DTX.b演奏で直前の音を消音する.Guitar = true;
					break;
				case 0x07:	// ギター消音無し
					CDTXMania.DTX.b演奏で直前の音を消音する.Guitar = false;
					break;
				case 0x08:	// ベース消音あり(従来同等)
					CDTXMania.DTX.b演奏で直前の音を消音する.Bass = true;
					break;
				case 0x09:	// ベース消音無し
					CDTXMania.DTX.b演奏で直前の音を消音する.Bass = false;
					break;
			}
#endif

		}
        protected override void t進行描画_チップ_ボーナス(CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip)
        {
            if (!pChip.bHit && (pChip.nバーからの距離dot.Drums < 0))
            {
                pChip.bHit = true;
            }
        }
#if false
		protected override void t進行描画_チップ_ベース( CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip )
		{
			if ( configIni.bGuitar有効 )
			{
				if ( configIni.bSudden.Bass )
				{
					pChip.b可視 = pChip.nバーからの距離dot.Bass < 200;
				}
				if ( configIni.bHidden.Bass && ( pChip.nバーからの距離dot.Bass < 100 ) )
				{
					pChip.b可視 = false;
				}
				if ( !pChip.bHit && pChip.b可視 )
				{
					int num8 = configIni.bReverse.Bass ? ( 0x171 - pChip.nバーからの距離dot.Bass ) : ( 40 + pChip.nバーからの距離dot.Bass );
					if ( ( num8 > 0 ) && ( num8 < 0x199 ) )
					{
						int num9 = this.ctチップ模様アニメ.Bass.n現在の値;
						if ( pChip.nチャンネル番号 == 160 )
						{
							if ( this.txチップ != null )
							{
								this.txチップ.t2D描画( CDTXMania.app.Device, 480, num8 - 4, new Rectangle( 0, 0xc0 + ( ( num9 % 5 ) * 8 ), 0x67, 8 ) );
							}
						}
						else if ( !configIni.bLeft.Bass )
						{
							Rectangle rectangle3 = new Rectangle( 0, num9 * 8, 0x20, 8 );
							if ( ( ( pChip.nチャンネル番号 & 4 ) != 0 ) && ( this.txチップ != null ) )
							{
								this.txチップ.t2D描画( CDTXMania.app.Device, 480, num8 - 4, rectangle3 );
							}
							rectangle3.X += 0x20;
							if ( ( ( pChip.nチャンネル番号 & 2 ) != 0 ) && ( this.txチップ != null ) )
							{
								this.txチップ.t2D描画( CDTXMania.app.Device, 0x204, num8 - 4, rectangle3 );
							}
							rectangle3.X += 0x20;
							if ( ( ( pChip.nチャンネル番号 & 1 ) != 0 ) && ( this.txチップ != null ) )
							{
								this.txチップ.t2D描画( CDTXMania.app.Device, 0x228, num8 - 4, rectangle3 );
							}
						}
						else
						{
							Rectangle rectangle4 = new Rectangle( 0, num9 * 8, 0x20, 8 );
							if ( ( ( pChip.nチャンネル番号 & 4 ) != 0 ) && ( this.txチップ != null ) )
							{
								this.txチップ.t2D描画( CDTXMania.app.Device, 0x228, num8 - 4, rectangle4 );
							}
							rectangle4.X += 0x20;
							if ( ( ( pChip.nチャンネル番号 & 2 ) != 0 ) && ( this.txチップ != null ) )
							{
								this.txチップ.t2D描画( CDTXMania.app.Device, 0x204, num8 - 4, rectangle4 );
							}
							rectangle4.X += 0x20;
							if ( ( ( pChip.nチャンネル番号 & 1 ) != 0 ) && ( this.txチップ != null ) )
							{
								this.txチップ.t2D描画( CDTXMania.app.Device, 480, num8 - 4, rectangle4 );
							}
						}
					}
				}
				if ( ( configIni.bAutoPlay.Bass && !pChip.bHit ) && ( pChip.nバーからの距離dot.Bass < 0 ) )
				{
					pChip.bHit = true;
					if ( ( ( pChip.nチャンネル番号 & 4 ) != 0 ) || ( pChip.nチャンネル番号 == 0xA0 ) )
					{
						this.actChipFireGB.Start( 3 );
					}
					if ( ( ( pChip.nチャンネル番号 & 2 ) != 0 ) || ( pChip.nチャンネル番号 == 0xA0 ) )
					{
						this.actChipFireGB.Start( 4 );
					}
					if ( ( ( pChip.nチャンネル番号 & 1 ) != 0 ) || ( pChip.nチャンネル番号 == 0xA0 ) )
					{
						this.actChipFireGB.Start( 5 );
					}
					this.tサウンド再生( pChip, CSound管理.rc演奏用タイマ.n前回リセットした時のシステム時刻 + pChip.n発声時刻ms, E楽器パート.BASS, dTX.nモニタを考慮した音量( E楽器パート.BASS ) );
					this.r次にくるベースChip = null;
					this.tチップのヒット処理( pChip.n発声時刻ms, pChip );
				}
				return;
			}
			if ( !pChip.bHit && ( pChip.nバーからの距離dot.Bass < 0 ) )
			{
				pChip.bHit = true;
				this.tサウンド再生( pChip, CSound管理.rc演奏用タイマ.n前回リセットした時のシステム時刻 + pChip.n発声時刻ms, E楽器パート.BASS, dTX.nモニタを考慮した音量( E楽器パート.BASS ) );
			}
		}
#endif
        protected override void t進行描画_チップ_ベース_ウェイリング( CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip )
		{
			if ( configIni.bGuitar有効 )
			{
				//if ( configIni.bSudden.Bass )
				//{
				//    pChip.b可視 = pChip.nバーからの距離dot.Bass < 200;
				//}
				//if ( configIni.bHidden.Bass && ( pChip.nバーからの距離dot.Bass < 100 ) )
				//{
				//    pChip.b可視 = false;
				//}

				//
				// 後日、以下の部分を何とかCStage演奏画面共通.csに移したい。
				//
				if ( !pChip.bHit && pChip.b可視 )
				{
					int[] y_base = { 154, 611 };			// ドラム画面かギター画面かで変わる値
					int offset = 0;						// ドラム画面かギター画面かで変わる値

					const int WailingWidth = 54;		// 4種全て同じ値
					const int WailingHeight = 68;		// 4種全て同じ値
					const int baseTextureOffsetX = 0;	// ドラム画面かギター画面かで変わる値
					const int baseTextureOffsetY = 22;	// ドラム画面かギター画面かで変わる値
					const int drawX = 1155;				// 4種全て異なる値

					const int numA = 34;				// 4種全て同じ値
					int y = CDTXMania.ConfigIni.bReverse.Bass ? ( y_base[ 1 ] - pChip.nバーからの距離dot.Bass ) : ( y_base[ 0 ] + pChip.nバーからの距離dot.Bass );
					int numB = y - offset;				// 4種全て同じ定義
					int numC = 0;						// 4種全て同じ初期値
					const int numD = 709;				// ドラム画面かギター画面かで変わる値
					if ( ( numB < ( numD + numA ) ) && ( numB > -numA ) )	// 以下のロジックは4種全て同じ
					{
						int c = this.ctWailingチップ模様アニメ.n現在の値;
                        Rectangle rect = new Rectangle(baseTextureOffsetX, baseTextureOffsetY, WailingWidth, WailingHeight);
						if ( numB < numA )
						{
							rect.Y += numA - numB;
							rect.Height -= numA - numB;
							numC = numA - numB;
						}
						if ( numB > ( numD - numA ) )
						{
							rect.Height -= numB - ( numD - numA );
						}
						if ( ( rect.Bottom > rect.Top ) && ( this.txチップ != null ) )
						{
                            this.txチップ.tDraw2D(CDTXMania.app.Device, drawX, (y - numA) + numC, rect);
						}
					}
				}
				//    if ( !pChip.bHit && ( pChip.nバーからの距離dot.Bass < 0 ) )
				//    {
				//        pChip.bHit = true;
				//        if ( configIni.bAutoPlay.Bass )
				//        {
				//            this.actWailingBonus.Start( EInstrumentPart.BASS, this.r現在の歓声Chip.Bass );
				//        }
				//    }
				//    return;
				//}
				//pChip.bHit = true;
				base.t進行描画_チップ_ベース_ウェイリング( configIni, ref dTX, ref pChip );
			}
		}
		protected override void t進行描画_チップ_空打ち音設定_ドラム( CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip )
		{
			if ( !pChip.bHit && ( pChip.nバーからの距離dot.Drums < 0 ) )
			{
				pChip.bHit = true;
			}
		}
		protected override void t進行描画_チップ_小節線( CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip )
		{
			int n小節番号plus1 = pChip.n発声位置 / 0x180;
			if ( !pChip.bHit && ( pChip.nバーからの距離dot.Drums < 0 ) )
			{
				pChip.bHit = true;
				this.actPlayInfo.n小節番号 = n小節番号plus1 - 1;
				if ( configIni.bWave再生位置自動調整機能有効 && bIsDirectSound )
				{
					dTX.tWave再生位置自動補正();
				}
			}
			if ( ( pChip.b可視 && configIni.bGuitar有効 ))
			{
                int y = CDTXMania.ConfigIni.bReverse.Guitar ? ((this.nJudgeLinePosY.Guitar - pChip.nバーからの距離dot.Guitar) + 0) : ((this.nJudgeLinePosY.Guitar + pChip.nバーからの距離dot.Guitar) + 9);
                if ( ( dTX.bチップがある.Guitar && ( y > 104 ) ) && ( ( y < 670 ) && ( this.txチップ != null ) ) )
                {
                    if( CDTXMania.ConfigIni.nLaneDisp.Guitar == 0 || CDTXMania.ConfigIni.nLaneDisp.Guitar == 1 )
					    this.txチップ.tDraw2D( CDTXMania.app.Device, 88, y, new Rectangle( 0, 20, 193, 2 ) );

                    if ( configIni.b演奏情報を表示する )
                    {
                        int n小節番号 = n小節番号plus1 - 1;
                        CDTXMania.act文字コンソール.tPrint(60, y - 16, C文字コンソール.Eフォント種別.白, n小節番号.ToString());
                    }
				}
                y = CDTXMania.ConfigIni.bReverse.Bass ? ((this.nJudgeLinePosY.Bass - pChip.nバーからの距離dot.Bass) + 0) : ((this.nJudgeLinePosY.Bass + pChip.nバーからの距離dot.Bass) + 9);
                if ( ( dTX.bチップがある.Bass && ( y > 104 ) ) && ( ( y < 670 ) && ( this.txチップ != null ) ) )
                {
                    if( CDTXMania.ConfigIni.nLaneDisp.Bass == 0 || CDTXMania.ConfigIni.nLaneDisp.Bass == 1 )
					    this.txチップ.tDraw2D( CDTXMania.app.Device, 959, y, new Rectangle( 0, 20, 193, 2 ) );

                    if ( configIni.b演奏情報を表示する )
                    {
                        int n小節番号 = n小節番号plus1 - 1;
                        CDTXMania.act文字コンソール.tPrint(930, y - 16, C文字コンソール.Eフォント種別.白, n小節番号.ToString());
                    }
				}
			}

		}
		#endregion
	}
}
