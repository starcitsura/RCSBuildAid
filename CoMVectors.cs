/* Copyright © 2013, Elián Hanisch <lambdae2@gmail.com>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace RCSBuildAid
{
    /* Component for calculate and show forces in CoM */
    public class CoMVectors : MonoBehaviour
    {
        VectorGraphic transVector;
        TorqueGraphic torqueCircle;
        float threshold = 0.05f;
        Vector3 torque = Vector3.zero;
        Vector3 translation = Vector3.zero;

        public float valueTorque {
            get { 
                if (torqueCircle == null) {
                    return 0f;
                }
                return torqueCircle.value.magnitude;
            }
        }

        public float valueTranslation {
            get {
                if (transVector == null) {
                    return 0f;
                }
                return transVector.value.magnitude; 
            }
        }

        public new bool enabled {
            get { return base.enabled; }
            set { 
                base.enabled = value;
                if (transVector == null || torqueCircle == null) {
                    return;
                }
                transVector.gameObject.SetActive (value);
                torqueCircle.gameObject.SetActive (value);
            }
        }

        void Awake ()
        {
            /* layer change must be done before adding the Graphic components */
            GameObject obj = new GameObject ("Translation Vector Object");
            obj.layer = gameObject.layer;
            obj.transform.parent = transform;
            obj.transform.localPosition = Vector3.zero;

            transVector = obj.AddComponent<VectorGraphic> ();
            transVector.width = 0.15f;
            transVector.color = Color.green;
            transVector.offset = 0.6f;
            transVector.maxLength = 3f;

            obj = new GameObject ("Torque Circle Object");
            obj.layer = gameObject.layer;
            obj.transform.parent = transform;
            obj.transform.localPosition = Vector3.zero;

            torqueCircle = obj.AddComponent<TorqueGraphic> ();
        }

        void Start ()
        {
            if (RCSBuildAid.Reference == gameObject) {
                /* we should start enabled */
                enabled = true;
            } else {
                enabled = false;
            }
        }

        Vector3 calcTorque (Transform transform, Vector3 force)
        {
            Vector3 lever = transform.position - this.transform.position;
            return Vector3.Cross (lever, force);
        }

        void sumForces<T> (List<PartModule> moduleList) where T : ModuleForces
        {
            foreach (PartModule mod in moduleList) {
                if (mod == null) {
                    continue;
                }
                ModuleForces mf = mod.GetComponent<T> ();
                if (mf == null || !mf.enabled) {
                    continue;
                }
                for (int t = 0; t < mf.vectors.Length; t++) {
                    Vector3 force = mf.vectors [t].value;
                    translation -= force;
                    torque += calcTorque (mf.vectors [t].transform, force);
                }
            }
        }

        void LateUpdate ()
        {
            /* calculate torque, translation and display them */
            torque = Vector3.zero;
            translation = Vector3.zero;

            switch(RCSBuildAid.mode) {
            case DisplayMode.RCS:
                sumForces<RCSForce> (RCSBuildAid.RCSlist);
                if (RCSBuildAid.rcsMode == RCSMode.ROTATION) {
                    /* rotation mode, we want to reduce translation */
                    torqueCircle.valueTarget = RCSBuildAid.Normal * -1;
                    transVector.valueTarget = Vector3.zero;
                } else {
                    /* translation mode, we want to reduce torque */
                    transVector.valueTarget = RCSBuildAid.Normal * -1;
                    torqueCircle.valueTarget = Vector3.zero;
                }
                break;
            case DisplayMode.Engine:
                sumForces<EngineForce> (RCSBuildAid.EngineList);
                torqueCircle.valueTarget = Vector3.zero;
                transVector.valueTarget = Vector3.zero;
                break;
            }

            /* update vectors in CoM */
            torqueCircle.value = torque;
            transVector.value = translation;

            torqueCircle.enabled = (torque.magnitude > threshold) ? true : false;
            transVector.enabled = (translation.magnitude > threshold) ? true : false;

            if (torque != Vector3.zero) {
                torqueCircle.transform.rotation = Quaternion.LookRotation (torque, translation);
            }
        }
    }
}

