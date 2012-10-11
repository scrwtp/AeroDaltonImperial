using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Robocode;
using Robocode.Util;
using scrwtp.AeroDaltonImperial.Common;

namespace scrwtp.AeroDaltonImperial
{
    public class AeroDaltonImperial : AdvancedRobot
    {
        /// <summary>
        /// Default move distance.
        /// </summary>
        private double defaultDistance;
        
        /// <summary>
        /// Maximum move distance (used as maximum radius in radius sampling move method).
        /// </summary>
        private double maximumDistance;

        /// <summary>
        /// Collection of enemy data snapshots.
        /// </summary>
        private Dictionary<string, EnemyData> enemies;
        
        /// <summary>
        /// Collection of predicted enemy bullets fired.
        /// </summary>
        private List<WaveBullet> enemyBullets;
        
        /// <summary>
        /// Module handling targeting statistics.
        /// </summary>
        private StatisticsModule statistics;
        
        /// <summary>
        /// Move method to use (anti-gravity movement or radius sampling method).
        /// </summary>
        private MovementMode currentMovementMode;

        /// <summary>
        /// Determines the shape of radar cone.
        /// </summary>
        private double radarFactor = 1.4;

        /// <summary>
        /// Previous move angle.
        /// </summary>
        private double previousAngle;

        /// <summary>
        /// Distinct guess factors to use (should be an odd number).
        /// </summary>
        private int guessFactors = 31;

        /// <summary>
        /// Time of the previous collision.
        /// </summary>
        private long collisionTime;

        /// <summary>
        /// Duration of panic mode after last collision.
        /// </summary>
        private int panicModeDuration = 20;

        /// <summary>
        /// Number of sample move targets to generate using radius sampling move method.
        /// </summary>
        private int moveTargetSampleSize;

        /// <summary>
        /// Width of boundary safe zone in pixels (movements within this zone carry substantial risk of hitting the walls).
        /// </summary>
        private int boundarySafeZone;

        /// <summary>
        /// Reference to an RNG.
        /// </summary>
        private Random randomizer;
        
        public AeroDaltonImperial()
            : base()
        {
            this.defaultDistance = 200;
            this.maximumDistance = 300;
            this.boundarySafeZone = 40;
            this.moveTargetSampleSize = 12;
            this.enemies = new Dictionary<string, EnemyData>();
            this.enemyBullets = new List<WaveBullet>();
            this.statistics = new StatisticsModule();
            this.randomizer = new Random();
        }

        /// <summary>
        /// Sets the colors of the robot 
        /// </summary>
        private void PaintJob()
        {
            this.BodyColor = System.Drawing.Color.FromArgb(97, 97, 85);
            this.GunColor = System.Drawing.Color.FromArgb(97, 94, 31);
            this.RadarColor = System.Drawing.Color.FromArgb(31, 25, 22);
            this.BulletColor = System.Drawing.Color.FromArgb(212, 0, 0);
        }

        /// <summary>
        /// The main method in every robot. You must override this to set up your
        /// robot's basic behavior.
        /// Contains the main loop.
        /// </summary>
        public override void Run()
        {
            try
            {
                PaintJob();

                this.IsAdjustGunForRobotTurn = true;
                this.IsAdjustRadarForRobotTurn = true;
                this.IsAdjustRadarForGunTurn = true;

                this.SetTurnRadarRightRadians(double.PositiveInfinity);

                // reset private variables
                this.previousAngle = 0;
                this.collisionTime = 0;
                this.currentMovementMode = MovementMode.AntiGravity;

                this.enemyBullets = new List<WaveBullet>();

                do
                {
                    // wait to gather enemy statistics before taking action
                    if (this.Time > 9)
                    {
                        Action();
                    }

                    Execute();
                } while (true);
            }
            catch (Exception e)
            {
                // for handling random exceptions that had sometimes occured at the end of round
            }
        }

        /// <summary>
        /// Do the moving and firing
        /// </summary>
        private void Action()
        {
            var location = new Point() { X = this.X, Y = this.Y };
            var time = this.Time;

            // sort enemies 
            var sortedEnemies = this.enemies.Select(kvp => kvp.Value).Where(x => time - x.Time < 30)
                .OrderBy(x => Point.Distance(location, x.Location));

            var enemyLocations = sortedEnemies.Select(x => x.Location).ToList();
            
            // can't tell which one of these two lines works better - the first was likely a bug
            // either way, a better target selection method is on order
            //var closestTarget = enemies.Select(x => x.Value).FirstOrDefault();
            var closestTarget = sortedEnemies.FirstOrDefault();

            // if close to move target or within the field boundary, take action
            if (this.DistanceRemaining < 15 || InBoundaryArea(location, this.BattleFieldHeight, this.BattleFieldWidth))
            {
                bool angleChange = false;
                double angle = 0;
                                
                if (time - collisionTime > this.panicModeDuration)
                {
                    // while in panic mode (after collision), use a mean angle from both movement methods
                    var antigravityAngle = Utils.NormalRelativeAngle(GetAntiGravityMoveTarget(location, enemyLocations, out angleChange));
                    var radiusSamplingAngle = Utils.NormalRelativeAngle(GetRadiusSamplingMoveTarget(location, closestTarget, enemyLocations, out angleChange));

                    angle = (antigravityAngle + radiusSamplingAngle / 2);
                }
                else if (this.currentMovementMode == MovementMode.AntiGravity)
                {
                    angle = Utils.NormalRelativeAngle(GetAntiGravityMoveTarget(location, enemyLocations, out angleChange));
                }
                else if (this.currentMovementMode == MovementMode.RadiusSampling)
                {
                    angle = Utils.NormalRelativeAngle(GetRadiusSamplingMoveTarget(location, closestTarget, enemyLocations, out angleChange));
                }
                
                // turn and move the default distance in the chosen direction
                if (Math.Abs(angle - this.HeadingRadians) < Math.PI / 2)
                {
                    this.SetTurnRightRadians(Utils.NormalRelativeAngle(angle - this.HeadingRadians));
                    this.SetAhead(this.defaultDistance);
                }
                else
                {
                    this.SetTurnRightRadians(Utils.NormalRelativeAngle(angle + Math.PI - this.HeadingRadians));
                    this.SetAhead(-this.defaultDistance);
                }

                this.previousAngle = angle;
            }

            // fire at the closest target
            if (closestTarget != null)
            {
                FireAtTarget(location, closestTarget);
            }

            // if fighting one-on-one, restore radar movement if the lock was broken
            if (this.GunHeat > 1.0 && this.Others == 1)
            {
                this.SetTurnRadarRightRadians(double.PositiveInfinity);
            }

            ProcessWaveBullets(location);
        }

        /// <summary>
        /// Gets the move target using radius sampling method
        /// </summary>
        /// <param name="location">The current location.</param>
        /// <param name="closestTarget">The closest target.</param>
        /// <param name="enemyLocations">Enemy locations.</param>
        /// <param name="angleChange">if set to <c>true</c>, a significant change of move angle has occured.</param>
        /// <returns></returns>
        private double GetRadiusSamplingMoveTarget(Point location, EnemyData closestTarget, List<Point> enemyLocations, out bool angleChange)
        {
            var potentialMoveTargets = GetPotentialMoveTargets(this.moveTargetSampleSize, location, enemyLocations, this.defaultDistance, this.maximumDistance, this.BattleFieldWidth, this.BattleFieldHeight);

            // move towards the closest target
            var moveTarget = potentialMoveTargets.OrderByDescending(x => Point.Distance(x, closestTarget.Location)).First();
            var angle = Math.Atan2(moveTarget.X - location.X, moveTarget.Y - location.Y);

            // not important if using this move method
            angleChange = false;
            return angle;
        }

        /// <summary>
        /// Gets the move target using anti-gravity method
        /// </summary>
        /// <param name="location">The current location.</param>
        /// <param name="enemyLocations">Enemy locations.</param>
        /// <param name="angleChange">if set to <c>true</c>, a significant change of move angle has occured.</param>
        /// <returns></returns>
        private double GetAntiGravityMoveTarget(Point location, List<Point> enemyLocations, out bool angleChange)
        {
            var force = GetAntiGravityForce(location, enemyLocations, this.enemyBullets, this.Time, this.BattleFieldWidth, this.BattleFieldHeight);
            var angle = Math.Atan2(force.X, force.Y);
            var deltaAngle = Math.Abs(this.previousAngle - angle);

            angleChange = deltaAngle > Math.PI / 16;
            return angle;
        }

        /// <summary>
        /// Fires at target.
        /// </summary>
        /// <param name="location">The current location.</param>
        /// <param name="target">The closest target.</param>
        /// <param name="direction">The direction.</param>
        private void FireAtTarget(Point location, EnemyData target)
        {
            var absBearing = target.BearingRadians + this.HeadingRadians;
            int direction = (Math.Sin(target.HeadingRadians - absBearing) * target.Velocity < 0) ? -1 : 1;     

            var distance = Point.Distance(location, target.Location);
            double power = GetFirePower(distance);
            
            var fieldArea = GetFieldArea(target.Location);
            var distanceBucket = (int)(distance / 100);

            var lastBullet = target.LastBullet;

            // initialize it to be in the middle, guessfactor 0.
            int bestindex = guessFactors / 2;	

            var stats = this.statistics.GetStatistics(target.Name);
            var bestStats = stats.SegmentedTargeting.Where(x => x.DistanceBucket == distanceBucket && x.FieldAreaBucket == fieldArea).OrderByDescending(x => x.Value).FirstOrDefault();

            if (bestStats != null)
            {
                bestindex = bestStats.GuessFactorBucket;
            }

            // this should do the opposite of the math in the WaveBullet
            double guessfactor = (double)(bestindex - (guessFactors - 1) / 2) / ((guessFactors - 1) / 2);
            double angleOffset = direction * guessfactor * lastBullet.MaxEscapeAngle();
            double gunAdjust = Utils.NormalRelativeAngle(lastBullet.StartBearing - this.GunHeadingRadians + angleOffset);

            this.SetTurnGunRightRadians(gunAdjust);

            if (this.GunHeat == 0 && gunAdjust < Math.Atan2(9, distance))
            {
                this.SetFireBullet(power);
            }
        }

        /// <summary>
        /// Processes the wave bullets, gathering targeting statistics
        /// </summary>
        /// <param name="location">The current location.</param>
        private void ProcessWaveBullets(Point location)
        {
            foreach (var enemyData in this.enemies.Values)
            {
                var targetStatistics = this.statistics.GetStatistics(enemyData.Name);

                var fieldArea = GetFieldArea(enemyData.Location);
                var distanceBucket = (int)(Point.Distance(enemyData.Location, location) / 100);

                var bulletsToTerminate = new List<WaveBullet>();

                // statistics for each enemy robot are segmented by distance to target and field area the enemy is in
                var currentStats = targetStatistics.SegmentedTargeting.Where(x => x.DistanceBucket == distanceBucket && x.FieldAreaBucket == fieldArea).ToList();

                foreach (var bullet in targetStatistics.WaveBullets)
                {
                    double guessFactor = 0;

                    if (bullet.CheckHit(enemyData.Name, enemyData.Location, enemyData.Time, out guessFactor))
                    {
                        int index = (int)Math.Round((guessFactors - 1) / 2 * (guessFactor + 1));

                        // increment the hit counter for the bucket
                        var stats = currentStats.Where(x => x.GuessFactorBucket == index).FirstOrDefault();

                        if (stats == null)
                        {
                            stats = new StatEntry()
                            {
                                GuessFactorBucket = index,
                                DistanceBucket = distanceBucket,
                                FieldAreaBucket = fieldArea,
                                Value = 1
                            };

                            targetStatistics.SegmentedTargeting.Add(stats);
                            currentStats.Add(stats);
                        }
                        else
                        {
                            stats.Value++;
                        }

                        // the bullet has hit, terminate it
                        bulletsToTerminate.Add(bullet);
                    }
                    else if (this.Time - bullet.Time > 30)
                    {
                        // terminate the old bullets
                        bulletsToTerminate.Add(bullet);
                    }
                }

                foreach (var bullet in bulletsToTerminate)
                {
                    targetStatistics.WaveBullets.Remove(bullet);
                }
            }
        }
        
        /// <summary>
        /// Handle robot death event.
        /// </summary>
        /// <param name="evnt"></param>
        /// <inheritdoc/>
        public override void OnRobotDeath(RobotDeathEvent evnt)
        {
            var name = evnt.Name;
            this.enemies.Remove(name);
        }

        /// <summary>
        /// Handle robot hit event.
        /// Use anti-gravity movement to move away from the hit robot
        /// </summary>
        /// <param name="evnt"></param>
        /// <inheritdoc/>
        public override void OnHitRobot(HitRobotEvent evnt)
        {
            this.currentMovementMode = MovementMode.AntiGravity;
            this.collisionTime = evnt.Time;
        }

        /// <summary>
        /// Handle wall hit event.
        /// Use radius sampling movement to ensure the next move position will move the robot away from the wall
        /// </summary>
        /// <param name="evnt"></param>
        /// <inheritdoc/>
        public override void OnHitWall(HitWallEvent evnt)
        {
            this.currentMovementMode = MovementMode.RadiusSampling;
            this.collisionTime = evnt.Time;
        }

        /// <summary>
        /// Handle bullet hit event.
        /// Use radius sampling movement to ensure the next move position will move the robot away from the wall
        /// </summary>
        /// <param name="evnt"></param>
        /// <inheritdoc/>
        public override void OnHitByBullet(HitByBulletEvent evnt)
        {
            this.currentMovementMode = MovementMode.RadiusSampling;
            this.collisionTime = evnt.Time;
        }

        /// <summary>
        /// Handle the scan event.
        /// 
        /// Predict whether the enemy has fired.
        /// Fire a wave bullet atth
        /// </summary>
        /// <param name="evnt"></param>
        /// <inheritdoc/>
        public override void OnScannedRobot(ScannedRobotEvent evnt)
        {
            var name = evnt.Name;
            var absBearing = evnt.BearingRadians + this.HeadingRadians;
            int direction = (Math.Sin(evnt.HeadingRadians - absBearing) * evnt.Velocity < 0) ? -1 : 1;            
            
            // if fighting one-on-one, maintain radar lock
            if (this.GunHeat < 1.0 && this.Others == 1)
            {
                var turn = this.HeadingRadians + evnt.BearingRadians - this.RadarHeadingRadians;
                this.SetTurnRadarRightRadians(this.radarFactor * Utils.NormalRelativeAngle(turn));
            }

            // get the updated enemy data snapshot
            var enemyData = new EnemyData()
            {
                Name = name,
                Location = new Point()
                {
                    X = this.X + Math.Sin(absBearing) * evnt.Distance,
                    Y = this.Y + Math.Cos(absBearing) * evnt.Distance
                },
                Time = evnt.Time,
                Energy = evnt.Energy,
                HeadingRadians = evnt.HeadingRadians,
                BearingRadians = evnt.BearingRadians,
                Velocity = evnt.Velocity
            };

            // predict whether the enemy has fired a bullet since last scan
            // skip this step if there are many robots left on the field to avoid noise
            if (this.Others <= 2)
            {
                if (enemies.ContainsKey(name))
                {
                    var energyDrop = enemies[name].Energy - enemyData.Energy;

                    if (energyDrop >= 0.1 && energyDrop <= 3)
                    {
                        this.enemyBullets.Add(new WaveBullet()
                        {
                            TargetName = this.Name,
                            StartPoint = enemyData.Location,
                            StartBearing = Utils.NormalRelativeAngle(Math.PI - absBearing),
                            Power = energyDrop,
                            Direction = direction,
                            Time = this.Time
                        });
                    }
                }
            }

            // update the enemy data
            this.enemies[name] = enemyData;

            // process targeting statistics 
            var targetStatistics = this.statistics.GetStatistics(name);
            
            // add the enemy data to pattern history for pattern matching targeting
            //targetStatistics.PatternHistory.Add(enemyData);

            // fire a wave bullet at the enemy
            double power = GetFirePower(evnt.Distance);
                      

            var location = new Point() { X = this.X, Y = this.Y };

            var waveBullet = new WaveBullet()
            {
                TargetName = name,
                StartPoint = location,
                StartBearing = absBearing,
                Power = power,
                Direction = direction,
                Time = this.Time
            };

            enemyData.LastBullet = waveBullet;
            targetStatistics.WaveBullets.Add(waveBullet);
        }

        /// <summary>
        /// Gets the gravity/anti-gravity force working on the robot.
        /// For more information, see Anti-Gravity Movement.
        /// </summary>
        /// <param name="currentLocation">The current location.</param>
        /// <param name="enemyLocations">Enemy locations.</param>
        /// <param name="enemyBullets">Predicted enemy bullets.</param>
        /// <param name="time">The time.</param>
        /// <param name="fieldWidth">Width of the field.</param>
        /// <param name="fieldHeight">Height of the field.</param>
        /// <returns></returns>
        private Force GetAntiGravityForce(Point currentLocation, List<Point> enemyLocations, List<WaveBullet> enemyBullets, long time, double fieldWidth, double fieldHeight)
        {
            var enemyGravityPoints = new List<GravityPoint>();
            var fieldGravityPoints = new List<GravityPoint>();

            var powerPerEnemy = -3000 / this.Others;

            // gravity points from enemies
            enemyGravityPoints.AddRange(enemyLocations.Select(point =>
                new GravityPoint()
                {
                    Location = point,
                    Power = powerPerEnemy,
                    DistanceDecay = 2
                }));

            // gravity points from walls (should minimise the risk of hitting them under normal circumstances)
            fieldGravityPoints.AddRange(new List<GravityPoint>() 
            {
                new GravityPoint() 
                {
                    Location = new Point() { X = 0, Y = currentLocation.Y },
                    Power = -5000,
                    DistanceDecay = 3
                },
                new GravityPoint() {
                    Location = new Point() { X = fieldWidth, Y = currentLocation.Y },
                    Power = -5000,
                    DistanceDecay = 3
                },
                new GravityPoint() {
                    Location = new Point() { X = currentLocation.X, Y = 0 },
                    Power = -5000,
                    DistanceDecay = 3
                },
                new GravityPoint() {
                    Location = new Point() { X =currentLocation.X, Y = fieldHeight },
                    Power = -5000,
                    DistanceDecay = 3
                },
            });

            // meant to add a perpendicular force from predicted enemy bullets that would push the robot aside, allowing to dodge them
            // never got around to get it to work though
            var enemyBulletGravityPoints = new List<GravityPoint>();
            if (this.Others <= 2)
            {
                var bulletsToTerminate = new List<WaveBullet>();

                foreach (var bullet in enemyBullets)
                {
                    double guessFactor = 0;
                    if (bullet.CheckHit(this.Name, currentLocation, this.Time + 10, out guessFactor))
                    {
                        enemyBulletGravityPoints.Add(new GravityPoint()
                        {
                            Power = 3000,
                            DistanceDecay = 0,
                            Location = bullet.StartPoint
                        });

                        bulletsToTerminate.Add(bullet);
                    }

                    if (this.Time - bullet.Time > 100)
                    {
                        bulletsToTerminate.Add(bullet);
                    }
                }

                foreach (var bullet in bulletsToTerminate)
                {
                    enemyBullets.Remove(bullet);
                }
            }

            var forceFromEnemies = GetNetForce(currentLocation, enemyGravityPoints);
            var forceFromField = GetNetForce(currentLocation, fieldGravityPoints);
            var forceFromEnemyBullets = GetNetForce(currentLocation, enemyBulletGravityPoints);

            // a dodge direction that would fluctuate with time
            var perpendicularDirection = ((this.Time / 20) % 2 == 0) ? 1 : -1;

            forceFromEnemyBullets = new Force()
            {
                X = perpendicularDirection * forceFromEnemyBullets.X,
                Y = -perpendicularDirection * forceFromEnemyBullets.Y
            };

            return new Force()
            {
                X = forceFromEnemies.X + forceFromField.X + forceFromEnemyBullets.X,
                Y = forceFromEnemies.Y + forceFromField.Y + forceFromEnemyBullets.Y
            };
        }

        /// <summary>
        /// Calculate the value of total force exherted at the location by the gravity points. 
        /// </summary>
        /// <param name="location">The current location.</param>
        /// <param name="gravityPoints">Points representing gravity/anti-gravity sources</param>
        /// <returns></returns>
        private Force GetNetForce(Point location, List<GravityPoint> gravityPoints)
        {
            var netForce = new Force() { X = 0.0, Y = 0.0 };

            foreach (var gravityPoint in gravityPoints)
            {
                var distance = Point.Distance(gravityPoint.Location, location);

                var force = gravityPoint.Power / Math.Pow(distance, gravityPoint.DistanceDecay);
                var angle = Utils.NormalAbsoluteAngle(Math.Atan2(gravityPoint.Location.X - location.X, gravityPoint.Location.Y - location.Y));

                netForce.X += Math.Sin(angle) * force;
                netForce.Y += Math.Cos(angle) * force;
            }

            return netForce;
        }

        /// <summary>
        /// Gets the potential move targets for the radius sampling move method.
        /// Radius sampling picks a number of points, one of which will be selected as the next move target
        /// </summary>
        /// <param name="moveTargetCount">The sample size.</param>
        /// <param name="location">The current location.</param>
        /// <param name="enemyLocations">Enemy locations.</param>
        /// <param name="defaultDistance">Default distance.</param>
        /// <param name="maximumDistance">Maximum distance.</param>
        /// <param name="fieldWidth">Width of the field.</param>
        /// <param name="fieldHeight">Height of the field.</param>
        /// <returns></returns>
        private List<Point> GetPotentialMoveTargets(int moveTargetCount, Point location, List<Point> enemyLocations, double defaultDistance, double maximumDistance, double fieldWidth, double fieldHeight)
        {
            var radius = Math.Max(defaultDistance, Math.Min(maximumDistance, enemyLocations.Min(x => Point.Distance(x, location))));

            var locations = new List<Point>();

            for (var i = 0; i < moveTargetCount || (i >= moveTargetCount && locations.Count < Math.Ceiling(moveTargetCount / 4.0)); i++)
            {
                var angle = 2 * Math.PI * this.randomizer.NextDouble();

                var point = new Point()
                {
                    X = location.X + Math.Cos(angle) * radius,
                    Y = location.Y + Math.Sin(angle) * radius
                };

                // skip the points that could lead to a wall hit
                if (!InBoundaryArea(point, fieldHeight, fieldWidth))
                {
                    locations.Add(point);
                }
            }

            return locations;
        }

        /// <summary>
        /// Calculates the fire power
        /// </summary>
        /// <param name="distance">The distance at which we are shooting.</param>
        /// <returns></returns>
        private double GetFirePower(double distance)
        {
            return Math.Min(3, Math.Max(.1, (800 / distance)));
        }

        /// <summary>
        /// Gets the field area for the given point
        /// Field areas correspond to numpad keys:
        /// an7 an8 an9
        /// an4 an5 an6
        /// an1 an2 an3
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns></returns>
        private FieldArea GetFieldArea(Point point)
        {
            int corner = 200;
            double bh = this.BattleFieldHeight, bw = this.BattleFieldWidth;

            int alignment = 9;

            if (point.X > corner && point.X <= bw - corner) alignment -= 1;
            else if (point.X < corner) alignment -= 2;

            if (point.Y > bh - corner) alignment -= 6;
            else if (point.Y > corner && point.Y <= bh - corner) alignment -= 3;

            return (FieldArea)alignment;
        }

        /// <summary>
        /// Checks if a point falls within the boundary area.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="height">The height.</param>
        /// <param name="width">The width.</param>
        /// <returns></returns>
        private bool InBoundaryArea(Point point, double height, double width)
        {
            if (point.X < this.boundarySafeZone || point.X > width - boundarySafeZone) return true;
            if (point.Y < this.boundarySafeZone || point.Y > height - boundarySafeZone) return true;
            return false;
        }
    }
}
