import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PingService } from './ping.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  constructor(public pingService: PingService){
    // pingService.ping().subscribe((response:any)=>{
    //   console.log(response.message)
    // })
  }
}
