import { Routes } from '@angular/router';
import { EventListComponent } from './components/event-list/event-list.component';
import { EventDetailComponent } from './components/event-detail/event-detail.component';

export const routes: Routes = [
  { path: '', component: EventListComponent },
  { path: 'events/:id', component: EventDetailComponent },
  { path: '**', redirectTo: '' }
];