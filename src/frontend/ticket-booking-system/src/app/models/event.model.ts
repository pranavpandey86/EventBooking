
export interface Event {
  eventId: string;
  name: string;
  description: string;
  category: string;
  eventDate: Date;
  location: string;
  maxCapacity: number;
  ticketPrice: number;
  organizerUserId: string;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
}
