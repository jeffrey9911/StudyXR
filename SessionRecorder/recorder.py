import socket
import csv
import datetime
import threading
import time

class UDPDataReceiver:
    def __init__(self, listening_port = 10000):
        self.listening_port = listening_port
        self.is_running = True
        self.sock = None
        self.receive_thread = None
        self.start_time = None

        timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
        self.csv_performance = f"session_{timestamp}_p.csv"
        self.csv_event = f"session_{timestamp}_e.csv"

        with open(self.csv_performance, 'w', newline='') as csvfile:
            writer = csv.writer(csvfile)
            writer.writerow
            writer.writerow(['time_ms', 'study_index','object_name', 'pos_x', 'pos_y', 'pos_z', 'rot_x', 'rot_y', 'rot_z'])
        
        with open(self.csv_event, 'w', newline='') as csvfile:
            writer = csv.writer(csvfile)
            writer.writerow
            writer.writerow(['time_ms', 'study_index', 'event_name', 'event_value'])
        
    def start_server(self):
        try:
            self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            self.sock.bind(('0.0.0.0', self.listening_port))

            if self.listening_port == 0:
                self.listening_port = self.sock.getsockname()[1]

            self.is_running = True
            self.start_time = time.time()

            self.receive_thread = threading.Thread(target=self.receive_data)
            self.receive_thread.daemon = True
            self.receive_thread.start()

            print(f"Transform data recorder started on port {self.listening_port}")
            print(f"Data will be saved to {self.csv_performance} and {self.csv_event}")

            return True
        except Exception as e:
            print(f"Error starting server: {e}")
            return False
        
    def receive_data(self):
        while self.is_running:
            try:
                data, addr = self.sock.recvfrom(1024)
                message = data.decode('utf-8')

                self.parse_and_record(message)
            except Exception as e:
                if self.is_running:
                    print(f"Error receiving data: {e}")

    def parse_and_record(self, message):
        print(f"Received message: {message}")
        try:
            time_ms = int((time.time() - self.start_time) * 1000)

            records = message.split(';')

            with open(self.csv_performance, 'a', newline='') as csv_p:
                with open(self.csv_event, 'a', newline='') as csv_e:
                    p_writer = csv.writer(csv_p)
                    e_writer = csv.writer(csv_e)

                    for record in records:
                        if not record:
                            continue

                        info = record.split(':')

                        if info[0] == 'PERF':
                            study_i = info[1]
                            data = info[2].split(',')
                            if len(data) != 7:
                                print(f"Invalid data format: {info[2]}")
                                continue
                            name = data[0]
                            pos_x, pox_y, pos_z = data[1], data[2], data[3]
                            rot_x, rot_y, rot_z = data[4], data[5], data[6]
                            p_writer.writerow([time_ms, study_i, name, pos_x, pox_y, pos_z, rot_x, rot_y, rot_z])
                            print(f"Recorded: {time_ms}, {study_i}, {name}, {pos_x}, {pox_y}, {pos_z}, {rot_x}, {rot_y}, {rot_z}")

                        if info[0] == 'EVNT':
                            study_i = info[1]
                            data = info[2].split(',')
                            if (len(data) != 2):
                                print(f"Invalid data format: {info[2]}")
                                continue
                            event = data[0]
                            value = data[1]
                            e_writer.writerow([time_ms, study_i, event, value])
                            print(f"Recorded: {time_ms}, {study_i}, {event}, {value}")

        except Exception as e:
            print(f"Error parsing data: {e}")

    def stop_server(self):
        self.is_running = False
        if self.sock:
            self.sock.close()

        if self.receive_thread and self.receive_thread.is_alive():
            self.receive_thread.join(1.0)

        print("Server stopped.")

# RUN

if __name__ == "__main__":
    port = 12588
    recorder = UDPDataReceiver(port)

    if recorder.start_server():
        try:
            while True:
                time.sleep(1)
        except KeyboardInterrupt:
            print("Stopping server...")
        finally:
            recorder.stop_server()
