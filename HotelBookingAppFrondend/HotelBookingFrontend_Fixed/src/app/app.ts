import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Menu } from './menu/menu';
import { Chatbot } from './chatbot/chatbot';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Menu, Chatbot],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  cities = ['Mumbai', 'Delhi', 'Goa', 'Bangalore', 'Jaipur', 'Chennai', 'Hyderabad', 'Kolkata'];
}
