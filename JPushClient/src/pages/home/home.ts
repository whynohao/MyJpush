import { Component } from '@angular/core';
import { NavController } from 'ionic-angular';
import { Helper } from '../../providers/Helper';

@Component({
  selector: 'page-home',
  templateUrl: 'home.html'
})
export class HomePage {

  constructor(public navCtrl: NavController) {

  }

  initJPush() { }

  getRegistrationID() {
    var onGetRegistradionID = function (data) {
      try {
        console.log("JPushPlugin:registrationID is " + data);
      } catch (exception) {
        console.log(exception);
      }
    };
    window["plugins"].jPushPlugin.getRegistrationID(onGetRegistradionID);
  }
}
