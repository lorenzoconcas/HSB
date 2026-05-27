import http from 'k6/http'

export const options = {
  stages: [
    { duration: '10s', target: 100 },
    { duration: '20s', target: 500 },
    { duration: '20s', target: 1000 },
    { duration: '10s', target: 0 },
  ],
}

export default function () {
  http.get('http://127.0.0.1:8080/health')
}
