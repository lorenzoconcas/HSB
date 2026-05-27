import http from 'k6/http'
import { check } from 'k6'

export const options = {
  vus: 200,
  duration: '60s',
}

export default function () {
  const res = http.get('http://127.0.0.1:8080/stream')

  check(res, {
    'status 200': (r) => r.status === 200,
    'chunked': (r) =>
      r.headers['Transfer-Encoding'] &&
      r.headers['Transfer-Encoding'].includes('chunked'),
    'body received': (r) => r.body && r.body.length > 0,
  })
}
